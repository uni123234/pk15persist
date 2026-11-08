using Project.Core.FileWorkspace.Canvas;
using Project.Core.FileWorkspace.FileSystem;
using Project.Core.FileWorkspace.States;

namespace Project.Core.FileWorkspace.Services;

/// <summary>
/// Runtime engine orchestrating the console workspace application loop and state graph traversal.
/// </summary>
public class WorkspaceEngine
{
    public WorkspaceContext Context { get; }

    public WorkspaceEngine(IFileSystemService? fileSystem = null, IConsoleCanvas? canvas = null, IWorkspaceState? initialState = null)
    {
        var fs = fileSystem ?? new RealFileSystemService();
        var cv = canvas ?? new VirtualConsoleCanvas();
        var state = initialState ?? new ExplorerState();

        Context = new WorkspaceContext(fs, cv, state);
    }

    /// <summary>
    /// Executes the interactive input loop until ShouldExit becomes true.
    /// </summary>
    public void RunInteractive(Func<string, string?> inputReader)
    {
        Context.Render();

        while (!Context.ShouldExit)
        {
            var line = inputReader("workspace> ");
            if (line == null)
            {
                break;
            }

            Context.ProcessInput(line);
        }
    }

    /// <summary>
    /// Executes a sequential batch of command inputs (for testing and scripted automation).
    /// </summary>
    public void ExecuteBatch(IEnumerable<string> commands)
    {
        foreach (var cmd in commands)
        {
            if (Context.ShouldExit)
            {
                break;
            }
            Context.ProcessInput(cmd);
        }
    }
}
