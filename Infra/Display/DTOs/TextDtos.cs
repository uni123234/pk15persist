using System.Text;
using Project.Core.Text.Models;

namespace Project.Infra.Display.DTOs;

/// <summary>
/// Data Transfer Object for rendering a Paragraph model.
/// </summary>
public class ParagraphDto(Paragraph paragraph, bool showId = false) : IDisplayable
{
    public int Id { get; } = paragraph.Id;
    public string Content { get; } = paragraph.Content;

    public string Render()
    {
        var idBadge = showId ? $"\u001b[90m[#{Id}]\u001b[0m " : "";
        return $"{idBadge}\u001b[37m{Content}\u001b[0m";
    }
}

/// <summary>
/// Data Transfer Object for rendering a Hyperlink element.
/// </summary>
public class TextLinkDto(TextLink link, bool showId = false) : IDisplayable
{
    public int Id { get; } = link.Id;
    public string Text { get; } = link.Text;
    public string Url { get; } = link.Url;

    public string Render()
    {
        var idBadge = showId ? $"\u001b[90m[#{Id}]\u001b[0m " : "";
        return $"{idBadge}\u001b[1;34m🔗 [{Text}]\u001b[0m(\u001b[4;36m{Url}\u001b[0m)";
    }
}

/// <summary>
/// Data Transfer Object for rendering an embedded Image element.
/// </summary>
public class TextImageDto(TextImage image, bool showId = false) : IDisplayable
{
    public int Id { get; } = image.Id;
    public string AltText { get; } = image.AltText;
    public string SourceUrl { get; } = image.SourceUrl;

    public string Render()
    {
        var idBadge = showId ? $"\u001b[90m[#{Id}]\u001b[0m " : "";
        return $"{idBadge}\u001b[1;33m🖼️ ![{AltText}]\u001b[0m(\u001b[90m{SourceUrl}\u001b[0m)";
    }
}

/// <summary>
/// Data Transfer Object for rendering a Heading and its hierarchical subtree.
/// Implements composite tree rendering with branch glyphs and colored ranks.
/// </summary>
public class HeadingDto(Heading heading, bool showId = false, bool tocOnly = false) : IDisplayable
{
    public int Id { get; } = heading.Id;
    public string Name { get; } = heading.Name;
    public int Rank { get; } = heading.Rank;
    public List<ITextElement> Children { get; } = heading.Children;

    public string Render()
    {
        var sb = new StringBuilder();
        RenderRecursive(sb, heading, "", true, showId, tocOnly, isRoot: true);
        return sb.ToString().TrimEnd();
    }

    private static void RenderRecursive(
        StringBuilder sb,
        Heading node,
        string indent,
        bool isLast,
        bool showId,
        bool tocOnly,
        bool isRoot)
    {
        var idBadge = showId ? $" \u001b[90m[#{node.Id}]\u001b[0m" : "";

        if (isRoot)
        {
            sb.AppendLine($"\u001b[1;36m📁 {node.Name}\u001b[0m{idBadge}");
        }
        else
        {
            var branch = isLast ? "└── " : "├── ";
            var rankColor = node.Rank switch
            {
                1 => "\u001b[1;32m",
                2 => "\u001b[1;33m",
                3 => "\u001b[1;35m",
                _ => "\u001b[1;37m"
            };
            sb.AppendLine($"{indent}{branch}{rankColor}📑 {node.Name}\u001b[0m{idBadge}");
        }

        var nextIndent = isRoot ? "" : indent + (isLast ? "    " : "│   ");

        var childrenToRender = tocOnly
            ? node.Children.OfType<Heading>().Cast<ITextElement>().ToList()
            : node.Children;

        for (var i = 0; i < childrenToRender.Count; i++)
        {
            var child = childrenToRender[i];
            var childIsLast = i == childrenToRender.Count - 1;

            if (child is Heading subHeading)
            {
                RenderRecursive(sb, subHeading, nextIndent, childIsLast, showId, tocOnly, isRoot: false);
            }
            else
            {
                var leafBranch = childIsLast ? "└── " : "├── ";
                var leafText = child switch
                {
                    Paragraph p => new ParagraphDto(p, showId).Render(),
                    TextLink l => new TextLinkDto(l, showId).Render(),
                    TextImage img => new TextImageDto(img, showId).Render(),
                    _ => child.Name
                };
                sb.AppendLine($"{nextIndent}{leafBranch}{leafText}");
            }
        }
    }
}
