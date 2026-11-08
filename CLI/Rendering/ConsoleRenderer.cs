using Project.Infra.Display;

namespace Project.CLI.Rendering;

/// <summary>
/// Production IRenderer implementation formatting and dispatching output to IDisplayer or Console.
/// </summary>
public class ConsoleRenderer : IRenderer
{
    private readonly IDisplayer? _displayer;

    public ConsoleRenderer(IDisplayer? displayer = null)
    {
        _displayer = displayer;
    }

    public void RenderLine(string text)
    {
        if (_displayer != null)
            _displayer.Display(text);
        else
            Console.WriteLine(text);
    }

    public void RenderRaw(string text)
    {
        if (_displayer != null)
            _displayer.Display(text);
        else
            Console.Write(text);
    }

    public void RenderTable(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var formatted = CLIGraphic.FormatTable(headers, rows);
        RenderRaw(formatted);
    }

    public void RenderProgressBar(int current, int total, string label = "")
    {
        var bar = CLIGraphic.FormatProgressBar(current, total, label);
        RenderLine(bar);
    }

    public void RenderBox(string title, IEnumerable<string> lines)
    {
        var box = CLIGraphic.FormatBox(title, lines);
        RenderRaw(box);
    }

    public void RenderStatus(string status, bool isSuccess)
    {
        var prefix = isSuccess ? "✔ [SUCCESS]" : "✖ [ERROR]";
        RenderLine($"{prefix} {status}");
    }
}

/// <summary>
/// In-memory IRenderer implementation capturing visual outputs for automated tests.
/// </summary>
public class BufferedRenderer : IRenderer
{
    public List<string> OutputLines { get; } = [];
    public List<string> StatusMessages { get; } = [];

    public void RenderLine(string text)
    {
        OutputLines.Add(text);
    }

    public void RenderRaw(string text)
    {
        var split = text.Replace("\r\n", "\n").Split('\n');
        foreach (var l in split)
        {
            if (!string.IsNullOrEmpty(l))
            {
                OutputLines.Add(l);
            }
        }
    }

    public void RenderTable(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var table = CLIGraphic.FormatTable(headers, rows);
        RenderRaw(table);
    }

    public void RenderProgressBar(int current, int total, string label = "")
    {
        var bar = CLIGraphic.FormatProgressBar(current, total, label);
        OutputLines.Add(bar);
    }

    public void RenderBox(string title, IEnumerable<string> lines)
    {
        var box = CLIGraphic.FormatBox(title, lines);
        RenderRaw(box);
    }

    public void RenderStatus(string status, bool isSuccess)
    {
        var prefix = isSuccess ? "✔ [SUCCESS]" : "✖ [ERROR]";
        var msg = $"{prefix} {status}";
        OutputLines.Add(msg);
        StatusMessages.Add(msg);
    }

    public bool ContainsText(string substring)
    {
        return OutputLines.Any(l => l.Contains(substring, StringComparison.OrdinalIgnoreCase));
    }

    public void Clear()
    {
        OutputLines.Clear();
        StatusMessages.Clear();
    }
}
