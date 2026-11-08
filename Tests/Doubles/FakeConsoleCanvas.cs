using Project.Core.FileWorkspace.Canvas;

namespace Tests.Doubles;

/// <summary>
/// [Testing Double: Fake]
/// In-memory implementation of IConsoleCanvas capturing virtual screen state for unit tests.
/// </summary>
public class FakeConsoleCanvas : IConsoleCanvas
{
    public int Width { get; } = 80;
    public int Height { get; } = 24;

    public string HeaderTitle { get; private set; } = "";
    public string HeaderMode { get; private set; } = "";
    public string HeaderPath { get; private set; } = "";
    public List<string> BodyLines { get; } = [];
    public string FooterControls { get; private set; } = "";
    public string? StatusMessage { get; private set; }

    public List<string> RenderedOutputs { get; } = [];
    public int FlushCount { get; private set; } = 0;

    public void Clear()
    {
        BodyLines.Clear();
        StatusMessage = null;
    }

    public void SetHeader(string title, string mode, string path)
    {
        HeaderTitle = title;
        HeaderMode = mode;
        HeaderPath = path;
    }

    public void SetBody(IEnumerable<string> lines)
    {
        BodyLines.Clear();
        BodyLines.AddRange(lines);
    }

    public void SetFooter(string controlsHint, string? statusMessage = null)
    {
        FooterControls = controlsHint;
        StatusMessage = statusMessage;
    }

    public string RenderToString()
    {
        var header = $"[{HeaderTitle}] MODE:{HeaderMode} PATH:{HeaderPath}";
        var body = string.Join("\n", BodyLines);
        var footer = $"STATUS:{StatusMessage} | CONTROLS:{FooterControls}";
        return $"{header}\n{body}\n{footer}";
    }

    public void Flush()
    {
        FlushCount++;
        RenderedOutputs.Add(RenderToString());
    }

    public bool ContainsInBody(string text)
    {
        return BodyLines.Any(b => b.Contains(text, StringComparison.OrdinalIgnoreCase));
    }
}
