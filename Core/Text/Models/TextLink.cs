namespace Project.Core.Text.Models;

/// <summary>
/// Leaf model representing a hyperlink within structured text with Prototype cloning support.
/// </summary>
public record TextLink(int Id, string Text, string Url) : ITextElement
{
    public string Name => Text;

    /// <summary>
    /// [Prototype Pattern] Clones link properties.
    /// </summary>
    public ITextElement Clone(bool deep = true) => new TextLink(Id, Text, Url);
}
