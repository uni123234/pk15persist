namespace Project.Core.FileWorkspace.FileSystem;

/// <summary>
/// Production file system service operating on the host operating system disk.
/// </summary>
public class RealFileSystemService : IFileSystemService
{
    private string _currentDirectory;

    public string CurrentDirectory => _currentDirectory;

    public RealFileSystemService(string? initialDirectory = null)
    {
        _currentDirectory = string.IsNullOrWhiteSpace(initialDirectory)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(initialDirectory);
    }

    public void ChangeDirectory(string path)
    {
        var target = Path.GetFullPath(Path.Combine(_currentDirectory, path));
        if (!Directory.Exists(target))
        {
            throw new DirectoryNotFoundException($"Directory '{target}' does not exist.");
        }
        _currentDirectory = target;
    }

    public IReadOnlyList<FileEntryInfo> GetEntries(string? directoryPath = null)
    {
        var dir = string.IsNullOrWhiteSpace(directoryPath) ? _currentDirectory : Path.GetFullPath(Path.Combine(_currentDirectory, directoryPath));
        if (!Directory.Exists(dir))
        {
            throw new DirectoryNotFoundException($"Directory '{dir}' does not exist.");
        }

        var result = new List<FileEntryInfo>();

        var dirInfo = new DirectoryInfo(dir);
        if (dirInfo.Parent != null)
        {
            result.Add(new FileEntryInfo("..", true, 0, dirInfo.LastWriteTimeUtc));
        }

        foreach (var subDir in dirInfo.GetDirectories())
        {
            result.Add(new FileEntryInfo(subDir.Name, true, 0, subDir.LastWriteTimeUtc));
        }

        foreach (var file in dirInfo.GetFiles())
        {
            result.Add(new FileEntryInfo(file.Name, false, file.Length, file.LastWriteTimeUtc));
        }

        return result;
    }

    public string ReadAllText(string filePath)
    {
        var full = GetFullPath(filePath);
        return File.ReadAllText(full);
    }

    public void WriteAllText(string filePath, string content)
    {
        var full = GetFullPath(filePath);
        var dir = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(full, content);
    }

    public bool FileExists(string filePath) => File.Exists(GetFullPath(filePath));

    public bool DirectoryExists(string dirPath) => Directory.Exists(GetFullPath(dirPath));

    public void CreateDirectory(string dirPath)
    {
        Directory.CreateDirectory(GetFullPath(dirPath));
    }

    public void DeleteFile(string filePath)
    {
        var full = GetFullPath(filePath);
        if (File.Exists(full))
        {
            File.Delete(full);
        }
    }

    public string CombinePath(params string[] paths) => Path.Combine(paths);

    public string GetFullPath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }
        return Path.GetFullPath(Path.Combine(_currentDirectory, path));
    }
}
