namespace Project.Core.FileWorkspace.States;

/// <summary>
/// [State 1: File Explorer Mode]
/// Displays the current directory hierarchy and dispatches file navigation and management commands.
/// Transition targets: ViewerState (view <file>), EditorState (edit <file>).
/// </summary>
public class ExplorerState : IWorkspaceState
{
    public string ModeName => "EXPLORER";

    public void Enter(WorkspaceContext context)
    {
        context.ActiveFilePath = null;
        context.EditorBuffer.Clear();
        context.IsBufferDirty = false;
        context.LastStatusMessage ??= "Browsing filesystem.";
    }

    public void Render(WorkspaceContext context)
    {
        var currentDir = context.FileSystem.CurrentDirectory;
        context.Canvas.SetHeader("Console File Explorer", ModeName, currentDir);

        var lines = new List<string>();
        try
        {
            var entries = context.FileSystem.GetEntries();
            if (entries.Count == 0)
            {
                lines.Add("  (Directory is empty)");
            }
            else
            {
                foreach (var entry in entries)
                {
                    var icon = entry.IsDirectory ? "📁 [DIR] " : "📄 [FILE]";
                    var name = entry.Name;
                    var size = entry.FormattedSize;
                    var date = entry.LastModified.ToString("yyyy-MM-dd HH:mm");
                    lines.Add($"  {icon} {name,-28} {size,10}  {date}");
                }
            }
        }
        catch (Exception ex)
        {
            lines.Add($"  [Error reading directory: {ex.Message}]");
        }

        context.Canvas.SetBody(lines);
        context.Canvas.SetFooter("Commands: cd <dir> | view <file> | edit <file> | mkdir <name> | touch <file> | rm <file> | exit", context.LastStatusMessage);
    }

    public void HandleInput(string input, WorkspaceContext context)
    {
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0].ToLowerInvariant();
        var arg = parts.Length > 1 ? parts[1].Trim() : "";

        switch (cmd)
        {
            case "cd":
                if (string.IsNullOrEmpty(arg))
                {
                    context.LastStatusMessage = "Usage: cd <directory>";
                }
                else
                {
                    try
                    {
                        context.FileSystem.ChangeDirectory(arg);
                        context.LastStatusMessage = $"Changed directory to {context.FileSystem.CurrentDirectory}";
                    }
                    catch (Exception ex)
                    {
                        context.LastStatusMessage = $"cd error: {ex.Message}";
                    }
                }
                context.Render();
                break;

            case "view":
            case "open":
                if (string.IsNullOrEmpty(arg))
                {
                    context.LastStatusMessage = "Usage: view <filename>";
                    context.Render();
                }
                else
                {
                    if (!context.FileSystem.FileExists(arg))
                    {
                        context.LastStatusMessage = $"File '{arg}' not found.";
                        context.Render();
                    }
                    else
                    {
                        context.TransitionTo(new ViewerState(arg));
                    }
                }
                break;

            case "edit":
                if (string.IsNullOrEmpty(arg))
                {
                    context.LastStatusMessage = "Usage: edit <filename>";
                    context.Render();
                }
                else
                {
                    context.TransitionTo(new EditorState(arg));
                }
                break;

            case "mkdir":
                if (string.IsNullOrEmpty(arg))
                {
                    context.LastStatusMessage = "Usage: mkdir <folder_name>";
                }
                else
                {
                    try
                    {
                        context.FileSystem.CreateDirectory(arg);
                        context.LastStatusMessage = $"Directory '{arg}' created.";
                    }
                    catch (Exception ex)
                    {
                        context.LastStatusMessage = $"mkdir error: {ex.Message}";
                    }
                }
                context.Render();
                break;

            case "touch":
                if (string.IsNullOrEmpty(arg))
                {
                    context.LastStatusMessage = "Usage: touch <filename>";
                }
                else
                {
                    try
                    {
                        if (!context.FileSystem.FileExists(arg))
                        {
                            context.FileSystem.WriteAllText(arg, "");
                            context.LastStatusMessage = $"File '{arg}' created.";
                        }
                        else
                        {
                            context.LastStatusMessage = $"File '{arg}' already exists.";
                        }
                    }
                    catch (Exception ex)
                    {
                        context.LastStatusMessage = $"touch error: {ex.Message}";
                    }
                }
                context.Render();
                break;

            case "rm":
            case "del":
                if (string.IsNullOrEmpty(arg))
                {
                    context.LastStatusMessage = "Usage: rm <filename>";
                }
                else
                {
                    try
                    {
                        if (context.FileSystem.FileExists(arg))
                        {
                            context.FileSystem.DeleteFile(arg);
                            context.LastStatusMessage = $"File '{arg}' deleted.";
                        }
                        else
                        {
                            context.LastStatusMessage = $"File '{arg}' not found.";
                        }
                    }
                    catch (Exception ex)
                    {
                        context.LastStatusMessage = $"rm error: {ex.Message}";
                    }
                }
                context.Render();
                break;

            case "exit":
            case "quit":
            case "q":
                context.ShouldExit = true;
                context.LastStatusMessage = "Exiting workspace.";
                break;

            default:
                context.LastStatusMessage = $"Unknown command '{cmd}'. Available: cd, view, edit, mkdir, touch, rm, exit.";
                context.Render();
                break;
        }
    }

    public void Exit(WorkspaceContext context) { }
}
