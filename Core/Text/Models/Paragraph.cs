using System.Text.Json.Serialization;

namespace Project.Core.Text.Models;

/// <summary>
/// Pure model representing a text paragraph with Prototype cloning support.
/// </summary>
public record Paragraph : ITextElement
{
    public int Id { get; init; }
    public string Content { get; init; }
    public string Name => $"Paragraph #{Id}";

    [JsonConstructor]
    public Paragraph(int id, string content)
    {
        Id = id;
        Content = content;
    }

    public Paragraph(string content) : this(0, content) { }

    /// <summary>
    /// [Prototype Pattern] Clones paragraph content.
    /// </summary>
    public ITextElement Clone(bool deep = true)
    {
        return new Paragraph(Id, Content);
    }
}
