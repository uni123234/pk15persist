using System.Text.RegularExpressions;

namespace Project.Infra.Caching;

/// <summary>
/// Local filesystem implementation of asset caching and buffering.
/// Stores character portraits locally to minimize network traffic and latency.
/// Employs atomic write operations (via temporary staging files) and graceful offline fallback generation.
/// </summary>
public class LocalFileAssetCacheService : IAssetCacheService
{
    private readonly string _cacheDirectory;
    private readonly HttpClient _httpClient;
    private int _cacheHits;
    private int _cacheMisses;

    // Minimal valid 67-byte 1x1 PNG image for offline fallback buffering
    private static readonly byte[] FallbackPngBytes =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR header
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // 1x1 px
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, // 8-bit RGBA
        0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, // IDAT header
        0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, 
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 
        0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, // IEND header
        0x42, 0x60, 0x82
    ];

    public string CacheDirectory => _cacheDirectory;

    public LocalFileAssetCacheService(string? cacheDirectory = null, HttpClient? httpClient = null)
    {
        _cacheDirectory = cacheDirectory ?? Path.Combine(AppContext.BaseDirectory, "cache", "portraits");
        Directory.CreateDirectory(_cacheDirectory);

        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    public bool IsCached(string assetKey)
    {
        var localPath = GetLocalFilePath(assetKey);
        return File.Exists(localPath) && new FileInfo(localPath).Length > 0;
    }

    public string GetLocalFilePath(string assetKey)
    {
        var safeKey = NormalizeKey(assetKey);
        var fileName = safeKey.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? safeKey : $"{safeKey}.png";
        return Path.Combine(_cacheDirectory, fileName);
    }

    public async Task<string> GetOrDownloadAssetAsync(string assetKey, string remoteUrl, CancellationToken ct = default)
    {
        var filePath = GetLocalFilePath(assetKey);

        if (IsCached(assetKey))
        {
            Interlocked.Increment(ref _cacheHits);
            return filePath;
        }

        Interlocked.Increment(ref _cacheMisses);

        byte[] data;
        try
        {
            data = await _httpClient.GetByteArrayAsync(remoteUrl, ct);
        }
        catch (Exception)
        {
            // Fallback generation for network disconnects or invalid URLs
            data = FallbackPngBytes;
        }

        WriteBytesAtomically(filePath, data);
        return filePath;
    }

    public async Task<string> GetOrDownloadPortraitAsync(string characterName, string? remoteUrl = null, CancellationToken ct = default)
    {
        var slug = NormalizeKey(characterName);
        var url = remoteUrl ?? $"https://genshin.jmp.blue/characters/{slug}/icon-big";
        return await GetOrDownloadAssetAsync(characterName, url, ct);
    }

    public async Task<IReadOnlyList<string>> PrefetchPortraitsAsync(IEnumerable<string> characterNames, CancellationToken ct = default)
    {
        var results = new List<string>();
        foreach (var name in characterNames)
        {
            var path = await GetOrDownloadPortraitAsync(name, ct: ct);
            results.Add(path);
        }
        return results;
    }

    public CacheStatistics GetStatistics()
    {
        var files = Directory.Exists(_cacheDirectory)
            ? Directory.GetFiles(_cacheDirectory, "*.*")
            : [];

        long totalSize = 0;
        foreach (var file in files)
        {
            try
            {
                totalSize += new FileInfo(file).Length;
            }
            catch
            {
                // Ignore transient file lock
            }
        }

        return new CacheStatistics(
            TotalCachedFiles: files.Length,
            TotalSizeBytes: totalSize,
            CacheHits: _cacheHits,
            CacheMisses: _cacheMisses,
            CacheDirectory: _cacheDirectory
        );
    }

    public void ClearCache()
    {
        if (Directory.Exists(_cacheDirectory))
        {
            foreach (var file in Directory.GetFiles(_cacheDirectory))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore locked files
                }
            }
        }

        Interlocked.Exchange(ref _cacheHits, 0);
        Interlocked.Exchange(ref _cacheMisses, 0);
    }

    private static void WriteBytesAtomically(string targetPath, byte[] data)
    {
        var dir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var tempPath = targetPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllBytes(tempPath, data);

        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        File.Move(tempPath, targetPath);
    }

    private static string NormalizeKey(string key)
    {
        var trimmed = key.Trim().ToLowerInvariant();
        var sanitized = Regex.Replace(trimmed, @"[^a-z0-9_-]", "-");
        return Regex.Replace(sanitized, @"-+", "-").Trim('-');
    }
}
