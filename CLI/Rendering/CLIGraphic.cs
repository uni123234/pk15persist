using System.Text;

namespace Project.CLI.Rendering;

/// <summary>
/// Reusable terminal graphic elements: formatted tables, progress bars, and framed alert boxes.
/// </summary>
public static class CLIGraphic
{
    public static string FormatTable(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        if (headers.Count == 0) return string.Empty;

        var colCount = headers.Count;
        var colWidths = new int[colCount];

        for (var i = 0; i < colCount; i++)
        {
            colWidths[i] = headers[i].Length;
        }

        foreach (var row in rows)
        {
            for (var i = 0; i < Math.Min(colCount, row.Count); i++)
            {
                colWidths[i] = Math.Max(colWidths[i], row[i].Length);
            }
        }

        // Add 2 padding chars to each column
        for (var i = 0; i < colCount; i++)
        {
            colWidths[i] += 2;
        }

        var sb = new StringBuilder();

        // Top border
        sb.Append('┌');
        for (var i = 0; i < colCount; i++)
        {
            sb.Append(new string('─', colWidths[i]));
            if (i < colCount - 1) sb.Append('┬');
        }
        sb.AppendLine("┐");

        // Headers
        sb.Append('│');
        for (var i = 0; i < colCount; i++)
        {
            sb.Append($" {headers[i]} ".PadRight(colWidths[i]));
            sb.Append('│');
        }
        sb.AppendLine();

        // Separator
        sb.Append('├');
        for (var i = 0; i < colCount; i++)
        {
            sb.Append(new string('─', colWidths[i]));
            if (i < colCount - 1) sb.Append('┼');
        }
        sb.AppendLine("┤");

        // Rows
        foreach (var row in rows)
        {
            sb.Append('│');
            for (var i = 0; i < colCount; i++)
            {
                var val = i < row.Count ? row[i] : "";
                sb.Append($" {val} ".PadRight(colWidths[i]));
                sb.Append('│');
            }
            sb.AppendLine();
        }

        // Bottom border
        sb.Append('└');
        for (var i = 0; i < colCount; i++)
        {
            sb.Append(new string('─', colWidths[i]));
            if (i < colCount - 1) sb.Append('┴');
        }
        sb.AppendLine("┘");

        return sb.ToString();
    }

    public static string FormatProgressBar(int current, int total, string label = "", int barWidth = 24)
    {
        total = Math.Max(1, total);
        current = Math.Clamp(current, 0, total);
        var ratio = (double)current / total;
        var percent = (int)(ratio * 100);
        var filledWidth = (int)(ratio * barWidth);
        var emptyWidth = barWidth - filledWidth;

        var bar = new string('█', filledWidth) + new string('░', emptyWidth);
        var text = string.IsNullOrEmpty(label) ? "" : $" {label}";
        return $"[{bar}] {percent,3}% ({current}/{total}){text}";
    }

    public static string FormatBox(string title, IEnumerable<string> lines, int minWidth = 50)
    {
        var lineList = lines.ToList();
        var maxLine = lineList.Count > 0 ? lineList.Max(l => l.Length) : 0;
        var width = Math.Max(minWidth, Math.Max(title.Length + 6, maxLine + 4));

        var sb = new StringBuilder();

        // Top border with title
        var titlePart = $"[ {title} ]";
        var leftDashes = (width - 2 - titlePart.Length) / 2;
        var rightDashes = width - 2 - titlePart.Length - leftDashes;
        sb.AppendLine($"╔{new string('═', Math.Max(0, leftDashes))}{titlePart}{new string('═', Math.Max(0, rightDashes))}╗");

        // Content
        foreach (var l in lineList)
        {
            sb.AppendLine($"║ {l.PadRight(width - 4)} ║");
        }

        // Bottom border
        sb.AppendLine($"╚{new string('═', width - 2)}╝");

        return sb.ToString();
    }
}
