using Project.Core.Text.Models;

namespace Project.Core.Text.Factories;

/// <summary>
/// Concrete factory generating standard plain text elements.
/// </summary>
public class PlainTextElementFactory : ITextElementFactory
{
    public string FormatName => "Plain Text";

    public Heading CreateHeading(int id, string name, int rank, int? parentId = null)
        => new(id, name, rank, [], parentId);

    public Paragraph CreateParagraph(int id, string content)
        => new(id, content);

    public TextLink CreateLink(int id, string text, string url)
        => new(id, $"{text} ({url})", url);

    public TextImage CreateImage(int id, string altText, string sourceUrl)
        => new(id, $"[Image: {altText}]", sourceUrl);
}

/// <summary>
/// Concrete factory generating elements formatted with Markdown syntax.
/// </summary>
public class MarkdownElementFactory : ITextElementFactory
{
    public string FormatName => "Markdown";

    public Heading CreateHeading(int id, string name, int rank, int? parentId = null)
    {
        var prefix = new string('#', Math.Clamp(rank, 1, 6));
        return new Heading(id, $"{prefix} {name}", rank, [], parentId);
    }

    public Paragraph CreateParagraph(int id, string content)
        => new(id, content);

    public TextLink CreateLink(int id, string text, string url)
        => new(id, $"[{text}]({url})", url);

    public TextImage CreateImage(int id, string altText, string sourceUrl)
        => new(id, $"![{altText}]({sourceUrl})", sourceUrl);
}

/// <summary>
/// Concrete factory generating elements formatted with semantic HTML tags.
/// </summary>
public class HtmlElementFactory : ITextElementFactory
{
    public string FormatName => "HTML";

    public Heading CreateHeading(int id, string name, int rank, int? parentId = null)
    {
        var tag = $"h{Math.Clamp(rank, 1, 6)}";
        return new Heading(id, $"<{tag}>{name}</{tag}>", rank, [], parentId);
    }

    public Paragraph CreateParagraph(int id, string content)
        => new(id, $"<p>{content}</p>");

    public TextLink CreateLink(int id, string text, string url)
        => new(id, $"<a href=\"{url}\">{text}</a>", url);

    public TextImage CreateImage(int id, string altText, string sourceUrl)
        => new(id, $"<img src=\"{sourceUrl}\" alt=\"{altText}\" />", sourceUrl);
}
