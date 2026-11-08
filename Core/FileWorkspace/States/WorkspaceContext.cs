using Project.Core.FileWorkspace.Canvas;
using Project.Core.FileWorkspace.FileSystem;

namespace Project.Core.FileWorkspace.States;

/// <summary>
/// Execution context and state machine coordinator for the Workspace application.
/// Maintains the active state, virtual canvas, file system abstraction, and document buffer.
/// </summary>
public class WorkspaceContext
{
    public IWorkspaceState CurrentState { get; private set; }
    public IFileSystemService FileSystem { get; }
    public IConsoleCanvas Canvas { get; }

    public string? ActiveFilePath { get; set; }
    public List<string> EditorBuffer { get; set; } = [];
    public bool IsBufferDirty { get; set; } = false;

    public int ViewerScrollOffset { get; set; } = 0;
    public int ViewerPageSize { get; set; } = 15;

    public string? LastStatusMessage { get; set; }
    public bool ShouldExit { get; set; } = false;

    public List<string> StateTransitionHistory { get; } = [];

    public WorkspaceContext(IFileSystemService fileSystem, IConsoleCanvas canvas, IWorkspaceState initialState)
    {
        FileSystem = fileSystem;
        Canvas = canvas;
        CurrentState = initialState;
        StateTransitionHistory.Add(initialState.ModeName);
        CurrentState.Enter(this);
    }

    /// <summary>
    /// Executes a clean state transition following the State pattern lifecycle (Exit old -> Enter new -> Render).
    /// </summary>
    public void TransitionTo(IWorkspaceState nextState)
    {
        ArgumentNullException.ThrowIfNull(nextState);
        CurrentState.Exit(this);
        CurrentState = nextState;
        StateTransitionHistory.Add(nextState.ModeName);
        CurrentState.Enter(this);
        Render();
    }

    public void Render()
    {
        CurrentState.Render(this);
        Canvas.Flush();
    }

    public void ProcessInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        CurrentState.HandleInput(input.Trim(), this);
    }
}
