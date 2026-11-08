namespace Project.Infra.Caching;

/// <summary>
/// Service contract defining local disk asset buffering and caching for character portraits.
/// Eliminates redundant HTTP requests and provides offline fallback capabilities.
/// </summary>
public interface IAssetCacheService
{
    /// <summary>
    /// Gets the absolute filesystem path to the local cache directory.
    /// </summary>
    string CacheDirectory { get; }

    /// <summary>
    /// Determines whether the specified asset is currently stored in the local cache.
    /// </summary>
    bool IsCached(string assetKey);

    /// <summary>
    /// Resolves the intended local file path for an asset key.
    /// </summary>
    string GetLocalFilePath(string assetKey);

    /// <summary>
    /// Checks the local disk cache; if missing, downloads from the remote URL and saves to disk.
    /// </summary>
    Task<string> GetOrDownloadAssetAsync(string assetKey, string remoteUrl, CancellationToken ct = default);

    /// <summary>
    /// Retrieves or buffers a character portrait from the remote CDN / API.
    /// </summary>
    Task<string> GetOrDownloadPortraitAsync(string characterName, string? remoteUrl = null, CancellationToken ct = default);

    /// <summary>
    /// Prefetches and buffers multiple character portraits in parallel.
    /// </summary>
    Task<IReadOnlyList<string>> PrefetchPortraitsAsync(IEnumerable<string> characterNames, CancellationToken ct = default);

    /// <summary>
    /// Gathers telemetry and diagnostic statistics about the cache.
    /// </summary>
    CacheStatistics GetStatistics();

    /// <summary>
    /// Clears all cached asset files from the disk and resets metrics.
    /// </summary>
    void ClearCache();
}
