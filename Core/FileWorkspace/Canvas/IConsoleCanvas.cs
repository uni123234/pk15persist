namespace Project.Core.FileWorkspace.Canvas;

/// <summary>
/// Abstraction for a virtual segmented terminal canvas (Header, Body, Footer).
/// Decouples UI rendering from direct console dependencies for high testability.
/// </summary>
public interface IConsoleCanvas
{
    int Width { get; }
    int Height { get; }

    void Clear();
    void SetHeader(string title, string mode, string path);
    void SetBody(IEnumerable<string> lines);
    void SetFooter(string controlsHint, string? statusMessage = null);

    string RenderToString();
    void Flush();
}
