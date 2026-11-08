using Project.Core.Text.Models;

namespace Project.Core.Text.Factories;

/// <summary>
/// [Creational Pattern: Abstract Factory]
/// Declares creation methods for families of structured text elements
/// tailored for distinct output formatting targets (Plain text, Markdown, HTML).
/// </summary>
public interface ITextElementFactory
{
    string FormatName { get; }

    Heading CreateHeading(int id, string name, int rank, int? parentId = null);
    Paragraph CreateParagraph(int id, string content);
    TextLink CreateLink(int id, string text, string url);
    TextImage CreateImage(int id, string altText, string sourceUrl);
}
