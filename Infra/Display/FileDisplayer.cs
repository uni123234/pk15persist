namespace Project.Infra.Display;

/// <summary>
/// Appends lines and displayable representations to a specified target file.
/// </summary>
public class FileDisplayer : IDisplayer
{
    private readonly string _filePath;

    public FileDisplayer(string filePath)
    {
        _filePath = filePath;
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public void Display(string line) => File.AppendAllText(_filePath, line + Environment.NewLine);

    public void Display(IDisplayable displayable) => File.AppendAllText(_filePath, displayable.Render() + Environment.NewLine);
}
