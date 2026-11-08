namespace Project.Core.FileWorkspace.States;

/// <summary>
/// [State 2: File Viewer Mode]
/// Displays file content with pagination and scrolling.
/// Transition targets: EditorState (edit), ExplorerState (back/q).
/// </summary>
public class ViewerState : IWorkspaceState
{
    public string ModeName => "VIEWER";
    public string FilePath { get; }
    private readonly List<string> _lines = [];

    public ViewerState(string filePath, IEnumerable<string>? cachedLines = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = filePath;
        if (cachedLines != null)
        {
            _lines.AddRange(cachedLines);
        }
    }

    public void Enter(WorkspaceContext context)
    {
        context.ActiveFilePath = FilePath;
        if (_lines.Count == 0 && context.FileSystem.FileExists(FilePath))
        {
            var content = context.FileSystem.ReadAllText(FilePath);
            var parsed = content.Replace("\r\n", "\n").Split('\n');
            _lines.AddRange(parsed);
        }

        context.ViewerScrollOffset = 0;
        context.LastStatusMessage = $"Opened '{FilePath}' in Viewer ({_lines.Count} lines).";
    }

    public void Render(WorkspaceContext context)
    {
        var totalLines = _lines.Count;
        var start = Math.Clamp(context.ViewerScrollOffset, 0, Math.Max(0, totalLines - 1));
        var count = Math.Min(context.ViewerPageSize, Math.Max(0, totalLines - start));

        var headerPath = $"{FilePath} (Lines {start + 1}-{start + count} of {totalLines})";
        context.Canvas.SetHeader("File Viewer", ModeName, headerPath);

        var body = new List<string>();
        if (totalLines == 0)
        {
            body.Add("  (File is completely empty)");
        }
        else
        {
            for (var i = start; i < start + count; i++)
            {
                var lineNum = i + 1;
                body.Add($" {lineNum,4} │ {_lines[i]}");
            }
        }

        context.Canvas.SetBody(body);
        context.Canvas.SetFooter("Controls: [n] Next Page | [p] Prev Page | [e] Edit | [q/back] Explorer | goto <num>", context.LastStatusMessage);
    }

    public void HandleInput(string input, WorkspaceContext context)
    {
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0].ToLowerInvariant();
        var arg = parts.Length > 1 ? parts[1].Trim() : "";

        switch (cmd)
        {
            case "n":
            case "next":
                if (context.ViewerScrollOffset + context.ViewerPageSize < _lines.Count)
                {
                    context.ViewerScrollOffset += context.ViewerPageSize;
                    context.LastStatusMessage = $"Scrolled down to line {context.ViewerScrollOffset + 1}.";
                }
                else
                {
                    context.LastStatusMessage = "Already reached end of file.";
                }
                context.Render();
                break;

            case "p":
            case "prev":
                if (context.ViewerScrollOffset > 0)
                {
                    context.ViewerScrollOffset = Math.Max(0, context.ViewerScrollOffset - context.ViewerPageSize);
                    context.LastStatusMessage = $"Scrolled up to line {context.ViewerScrollOffset + 1}.";
                }
                else
                {
                    context.LastStatusMessage = "Already at top of file.";
                }
                context.Render();
                break;

            case "e":
            case "edit":
                context.TransitionTo(new EditorState(FilePath, _lines));
                break;

            case "q":
            case "back":
            case "close":
                context.TransitionTo(new ExplorerState());
                break;

            case "goto":
                if (int.TryParse(arg, out var lineNum) && lineNum >= 1 && lineNum <= _lines.Count)
                {
                    context.ViewerScrollOffset = lineNum - 1;
                    context.LastStatusMessage = $"Jumped to line {lineNum}.";
                }
                else
                {
                    context.LastStatusMessage = $"Invalid line number. Enter a number between 1 and {_lines.Count}.";
                }
                context.Render();
                break;

            default:
                context.LastStatusMessage = $"Unknown viewer control '{cmd}'. Controls: [n] Next, [p] Prev, [e] Edit, [q] Back, goto <line>.";
                context.Render();
                break;
        }
    }

    public void Exit(WorkspaceContext context) { }
}
