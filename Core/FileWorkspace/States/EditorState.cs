namespace Project.Core.FileWorkspace.States;

/// <summary>
/// [State 3: File Editor Mode]
/// Provides in-memory document line editing (insert, append, delete, replace, save).
/// Transition targets: ViewerState (view/v), ExplorerState (close/q).
/// </summary>
public class EditorState : IWorkspaceState
{
    public string ModeName => "EDITOR";
    public string FilePath { get; private set; }

    public EditorState(string filePath, IEnumerable<string>? initialLines = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = filePath;
        if (initialLines != null)
        {
            InitialCachedLines = new List<string>(initialLines);
        }
    }

    private List<string>? InitialCachedLines { get; }

    public void Enter(WorkspaceContext context)
    {
        context.ActiveFilePath = FilePath;

        if (InitialCachedLines != null)
        {
            context.EditorBuffer.Clear();
            context.EditorBuffer.AddRange(InitialCachedLines);
        }
        else if (context.EditorBuffer.Count == 0 && context.FileSystem.FileExists(FilePath))
        {
            var content = context.FileSystem.ReadAllText(FilePath);
            var parsed = content.Replace("\r\n", "\n").Split('\n');
            context.EditorBuffer.Clear();
            context.EditorBuffer.AddRange(parsed);
        }

        context.LastStatusMessage = $"Editing '{FilePath}' ({context.EditorBuffer.Count} lines).";
    }

    public void Render(WorkspaceContext context)
    {
        var dirtyIndicator = context.IsBufferDirty ? " * [MODIFIED]" : " [SAVED]";
        var headerPath = $"{FilePath}{dirtyIndicator}";
        context.Canvas.SetHeader("File Editor", ModeName, headerPath);

        var body = new List<string>();
        if (context.EditorBuffer.Count == 0)
        {
            body.Add("  (Buffer is empty. Type 'add <text>' to append the first line)");
        }
        else
        {
            for (var i = 0; i < context.EditorBuffer.Count; i++)
            {
                var lineNum = i + 1;
                body.Add($" {lineNum,3} │ {context.EditorBuffer[i]}");
            }
        }

        context.Canvas.SetBody(body);
        context.Canvas.SetFooter("Commands: add <txt> | ins <n> <txt> | del <n> | set <n> <txt> | save/w | view/v | close/q", context.LastStatusMessage);
    }

    public void HandleInput(string input, WorkspaceContext context)
    {
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0].ToLowerInvariant();
        var rest = parts.Length > 1 ? parts[1].Trim() : "";

        switch (cmd)
        {
            case "add":
            case "append":
            case "a":
                context.EditorBuffer.Add(rest);
                context.IsBufferDirty = true;
                context.LastStatusMessage = $"Appended line {context.EditorBuffer.Count}.";
                context.Render();
                break;

            case "ins":
            case "insert":
            case "i":
            {
                var subParts = rest.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (subParts.Length < 2 || !int.TryParse(subParts[0], out var lineNum) || lineNum < 1 || lineNum > context.EditorBuffer.Count + 1)
                {
                    context.LastStatusMessage = $"Usage: ins <1..{context.EditorBuffer.Count + 1}> <text>";
                }
                else
                {
                    context.EditorBuffer.Insert(lineNum - 1, subParts[1]);
                    context.IsBufferDirty = true;
                    context.LastStatusMessage = $"Inserted line at position {lineNum}.";
                }
                context.Render();
                break;
            }

            case "del":
            case "delete":
            case "d":
            {
                if (!int.TryParse(rest, out var lineNum) || lineNum < 1 || lineNum > context.EditorBuffer.Count)
                {
                    context.LastStatusMessage = $"Usage: del <1..{context.EditorBuffer.Count}>";
                }
                else
                {
                    context.EditorBuffer.RemoveAt(lineNum - 1);
                    context.IsBufferDirty = true;
                    context.LastStatusMessage = $"Deleted line {lineNum}.";
                }
                context.Render();
                break;
            }

            case "set":
            case "replace":
            case "r":
            {
                var subParts = rest.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (subParts.Length < 2 || !int.TryParse(subParts[0], out var lineNum) || lineNum < 1 || lineNum > context.EditorBuffer.Count)
                {
                    context.LastStatusMessage = $"Usage: set <1..{context.EditorBuffer.Count}> <new_text>";
                }
                else
                {
                    context.EditorBuffer[lineNum - 1] = subParts[1];
                    context.IsBufferDirty = true;
                    context.LastStatusMessage = $"Updated line {lineNum}.";
                }
                context.Render();
                break;
            }

            case "clear":
                context.EditorBuffer.Clear();
                context.IsBufferDirty = true;
                context.LastStatusMessage = "Cleared all buffer content.";
                context.Render();
                break;

            case "save":
            case "w":
                try
                {
                    var text = string.Join(Environment.NewLine, context.EditorBuffer);
                    context.FileSystem.WriteAllText(FilePath, text);
                    context.IsBufferDirty = false;
                    context.LastStatusMessage = $"Saved {context.EditorBuffer.Count} lines to '{FilePath}'.";
                }
                catch (Exception ex)
                {
                    context.LastStatusMessage = $"Save error: {ex.Message}";
                }
                context.Render();
                break;

            case "saveas":
                if (string.IsNullOrEmpty(rest))
                {
                    context.LastStatusMessage = "Usage: saveas <new_file_path>";
                }
                else
                {
                    try
                    {
                        var text = string.Join(Environment.NewLine, context.EditorBuffer);
                        context.FileSystem.WriteAllText(rest, text);
                        FilePath = rest;
                        context.ActiveFilePath = rest;
                        context.IsBufferDirty = false;
                        context.LastStatusMessage = $"Saved buffer to new path '{rest}'.";
                    }
                    catch (Exception ex)
                    {
                        context.LastStatusMessage = $"saveas error: {ex.Message}";
                    }
                }
                context.Render();
                break;

            case "v":
            case "view":
                context.TransitionTo(new ViewerState(FilePath, context.EditorBuffer));
                break;

            case "close!":
            case "q!":
                context.IsBufferDirty = false;
                context.TransitionTo(new ExplorerState());
                break;

            case "close":
            case "q":
            case "back":
                if (context.IsBufferDirty)
                {
                    context.LastStatusMessage = "Warning: Unsaved changes! Use 'save' or 'close!' to discard.";
                    context.Render();
                }
                else
                {
                    context.TransitionTo(new ExplorerState());
                }
                break;

            default:
                context.LastStatusMessage = $"Unknown editor command '{cmd}'. Available: add, ins, del, set, save, view, close.";
                context.Render();
                break;
        }
    }

    public void Exit(WorkspaceContext context) { }
}
