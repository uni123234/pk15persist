namespace Project.Core.FileWorkspace.States;

/// <summary>
/// [Behavioral Pattern: State]
/// Contract for distinct workspace operational modes (Explorer, Viewer, Editor).
/// Encapsulates mode-specific UI rendering and contextual input command dispatch.
/// </summary>
public interface IWorkspaceState
{
    string ModeName { get; }

    void Enter(WorkspaceContext context);
    void Render(WorkspaceContext context);
    void HandleInput(string input, WorkspaceContext context);
    void Exit(WorkspaceContext context);
}
