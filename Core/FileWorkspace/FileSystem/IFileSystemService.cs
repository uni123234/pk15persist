namespace Project.Core.FileWorkspace.FileSystem;

/// <summary>
/// Metadata describing a file system entity (directory or file).
/// </summary>
public record FileEntryInfo(string Name, bool IsDirectory, long Length, DateTime LastModified)
{
    public string FormattedSize => IsDirectory ? "<DIR>" : $"{Length:N0} B";
}

/// <summary>
/// Abstraction layer over file system I/O.
/// Allows swapping real disk operations with in-memory test doubles.
/// </summary>
public interface IFileSystemService
{
    string CurrentDirectory { get; }

    void ChangeDirectory(string path);
    IReadOnlyList<FileEntryInfo> GetEntries(string? directoryPath = null);
    string ReadAllText(string filePath);
    void WriteAllText(string filePath, string content);
    bool FileExists(string filePath);
    bool DirectoryExists(string dirPath);
    void CreateDirectory(string dirPath);
    void DeleteFile(string filePath);
    string CombinePath(params string[] paths);
    string GetFullPath(string path);
}
