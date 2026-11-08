using System.Text;
using Project.Infra.Display;

namespace Project.Core.FileWorkspace.Canvas;

/// <summary>
/// Structured virtual console canvas dividing the terminal window into Header, Body, and Footer segments.
/// Implements ASCII/Unicode border rendering and optional ANSI escape sequences.
/// </summary>
public class VirtualConsoleCanvas : IConsoleCanvas
{
    private readonly IDisplayer? _displayer;
    private string _headerTitle = "System Workspace";
    private string _headerMode = "IDLE";
    private string _headerPath = "/";
    private readonly List<string> _bodyLines = [];
    private string _footerControls = "";
    private string? _statusMessage;

    public int Width { get; }
    public int Height { get; }

    public VirtualConsoleCanvas(int width = 80, int height = 24, IDisplayer? displayer = null)
    {
        Width = Math.Max(40, width);
        Height = Math.Max(10, height);
        _displayer = displayer;
    }

    public void Clear()
    {
        _bodyLines.Clear();
        _statusMessage = null;
    }

    public void SetHeader(string title, string mode, string path)
    {
        _headerTitle = title;
        _headerMode = mode;
        _headerPath = path;
    }

    public void SetBody(IEnumerable<string> lines)
    {
        _bodyLines.Clear();
        _bodyLines.AddRange(lines);
    }

    public void SetFooter(string controlsHint, string? statusMessage = null)
    {
        _footerControls = controlsHint;
        _statusMessage = statusMessage;
    }

    public string RenderToString()
    {
        var sb = new StringBuilder();
        var innerWidth = Width - 2;

        // Top Border
        sb.AppendLine($"╔{new string('═', innerWidth)}╗");

        // Header Line 1: Title & Mode
        var titleSegment = $" [{_headerTitle}] ";
        var modeSegment = $" [MODE: {_headerMode}] ";
        var spacesMiddle = Math.Max(1, innerWidth - titleSegment.Length - modeSegment.Length);
        sb.AppendLine($"║{titleSegment}{new string(' ', spacesMiddle)}{modeSegment}║");

        // Header Line 2: Path
        var pathText = $" Path: {_headerPath}";
        if (pathText.Length > innerWidth)
        {
            pathText = pathText[..Math.Max(0, innerWidth - 3)] + "...";
        }
        sb.AppendLine($"║{pathText.PadRight(innerWidth)}║");

        // Separator between Header and Body
        sb.AppendLine($"╠{new string('─', innerWidth)}╣");

        // Body Lines
        var visibleBodyCapacity = Height - 8;
        var renderedBodyCount = Math.Min(_bodyLines.Count, visibleBodyCapacity);

        for (var i = 0; i < renderedBodyCount; i++)
        {
            var line = _bodyLines[i];
            if (line.Length > innerWidth)
            {
                line = line[..Math.Max(0, innerWidth - 3)] + "...";
            }
            sb.AppendLine($"║ {line.PadRight(innerWidth - 1)}║");
        }

        // Fill remaining body height if any
        for (var i = renderedBodyCount; i < visibleBodyCapacity; i++)
        {
            sb.AppendLine($"║{new string(' ', innerWidth)}║");
        }

        // Separator between Body and Footer
        sb.AppendLine($"╠{new string('─', innerWidth)}╣");

        // Status Line
        if (!string.IsNullOrWhiteSpace(_statusMessage))
        {
            var statusText = $" Status: {_statusMessage}";
            if (statusText.Length > innerWidth)
            {
                statusText = statusText[..Math.Max(0, innerWidth - 3)] + "...";
            }
            sb.AppendLine($"║{statusText.PadRight(innerWidth)}║");
        }
        else
        {
            sb.AppendLine($"║{" Status: Ready".PadRight(innerWidth)}║");
        }

        // Controls Line
        var controlsText = $" {_footerControls}";
        if (controlsText.Length > innerWidth)
        {
            controlsText = controlsText[..Math.Max(0, innerWidth - 3)] + "...";
        }
        sb.AppendLine($"║{controlsText.PadRight(innerWidth)}║");

        // Bottom Border
        sb.AppendLine($"╚{new string('═', innerWidth)}╝");

        return sb.ToString();
    }

    public void Flush()
    {
        var output = RenderToString();
        if (_displayer != null)
        {
            _displayer.Display(output);
        }
        else
        {
            Console.WriteLine(output);
        }
    }
}
