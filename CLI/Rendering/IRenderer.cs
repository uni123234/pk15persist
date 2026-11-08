namespace Project.CLI.Rendering;

/// <summary>
/// Visual presentation strategy interface for the CLI module.
/// Decouples commands and domain entities from concrete console APIs.
/// </summary>
public interface IRenderer
{
    void RenderLine(string text);
    void RenderRaw(string text);
    void RenderTable(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows);
    void RenderProgressBar(int current, int total, string label = "");
    void RenderBox(string title, IEnumerable<string> lines);
    void RenderStatus(string status, bool isSuccess);
}

/// <summary>
/// Contract for domain models and DTOs that can render their visual representation through an IRenderer.
/// Implements Dependency Inversion: internal models define the capability, CLI implements the rendering strategy.
/// </summary>
public interface IRenderable
{
    void Render(IRenderer renderer);
}
