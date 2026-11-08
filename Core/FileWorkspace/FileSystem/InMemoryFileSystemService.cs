namespace Project.Core.FileWorkspace.FileSystem;

/// <summary>
/// In-memory mockable file system service for deterministic unit testing without disk I/O.
/// </summary>
public class InMemoryFileSystemService : IFileSystemService
{
    private string _currentDirectory = "/";
    private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);

    public string CurrentDirectory => _currentDirectory;

    public InMemoryFileSystemService()
    {
        _directories.Add("/");
    }

    private static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";
        var norm = path.Replace('\\', '/').Trim();
        if (!norm.StartsWith('/')) norm = "/" + norm;
        while (norm.Contains("//")) norm = norm.Replace("//", "/");
        if (norm.Length > 1 && norm.EndsWith('/')) norm = norm[..^1];
        return norm;
    }

    public void ChangeDirectory(string path)
    {
        var target = CombinePath(_currentDirectory, path);
        if (!_directories.Contains(target))
        {
            throw new DirectoryNotFoundException($"Directory '{target}' not found in virtual filesystem.");
        }
        _currentDirectory = target;
    }

    public IReadOnlyList<FileEntryInfo> GetEntries(string? directoryPath = null)
    {
        var dir = Normalize(directoryPath ?? _currentDirectory);
        if (!_directories.Contains(dir))
        {
            throw new DirectoryNotFoundException($"Directory '{dir}' not found.");
        }

        var result = new List<FileEntryInfo>();

        if (dir != "/")
        {
            result.Add(new FileEntryInfo("..", true, 0, DateTime.UtcNow));
        }

        // Subdirectories directly inside dir
        foreach (var d in _directories)
        {
            if (d == dir || d == "/") continue;
            var parent = GetParentDirectory(d);
            if (parent == dir)
            {
                var name = d.Substring(d.LastIndexOf('/') + 1);
                result.Add(new FileEntryInfo(name, true, 0, DateTime.UtcNow));
            }
        }

        // Files directly inside dir
        foreach (var (fPath, content) in _files)
        {
            var parent = GetParentDirectory(fPath);
            if (parent == dir)
            {
                var name = fPath.Substring(fPath.LastIndexOf('/') + 1);
                result.Add(new FileEntryInfo(name, false, content.Length, DateTime.UtcNow));
            }
        }

        return result.OrderByDescending(e => e.IsDirectory).ThenBy(e => e.Name).ToList();
    }

    public string ReadAllText(string filePath)
    {
        var full = GetFullPath(filePath);
        if (_files.TryGetValue(full, out var content))
        {
            return content;
        }
        throw new FileNotFoundException($"Virtual file '{full}' not found.");
    }

    public void WriteAllText(string filePath, string content)
    {
        var full = GetFullPath(filePath);
        var dir = GetParentDirectory(full);
        CreateDirectory(dir);
        _files[full] = content;
    }

    public bool FileExists(string filePath) => _files.ContainsKey(GetFullPath(filePath));

    public bool DirectoryExists(string dirPath) => _directories.Contains(GetFullPath(dirPath));

    public void CreateDirectory(string dirPath)
    {
        var full = GetFullPath(dirPath);
        var parts = full.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var current = "";
        _directories.Add("/");
        foreach (var part in parts)
        {
            current += "/" + part;
            _directories.Add(current);
        }
    }

    public void DeleteFile(string filePath)
    {
        _files.Remove(GetFullPath(filePath));
    }

    public string CombinePath(params string[] paths)
    {
        var combined = string.Join("/", paths.Select(p => p.Replace('\\', '/').Trim('/')));
        return Normalize(combined);
    }

    public string GetFullPath(string path)
    {
        var p = path.Replace('\\', '/');
        if (p.StartsWith('/'))
        {
            return Normalize(p);
        }
        if (p == "..")
        {
            return GetParentDirectory(_currentDirectory);
        }
        if (p.StartsWith("../"))
        {
            var parent = GetParentDirectory(_currentDirectory);
            return CombinePath(parent, p.Substring(3));
        }
        return CombinePath(_currentDirectory, p);
    }

    private static string GetParentDirectory(string path)
    {
        var norm = Normalize(path);
        if (norm == "/") return "/";
        var idx = norm.LastIndexOf('/');
        return idx <= 0 ? "/" : norm[..idx];
    }
}
