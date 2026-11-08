namespace Project.Core.Text.Models;

/// <summary>
/// Leaf model representing an embedded image reference with Prototype cloning support.
/// </summary>
public record TextImage(int Id, string AltText, string SourceUrl) : ITextElement
{
    public string Name => AltText;

    /// <summary>
    /// [Prototype Pattern] Clones image properties.
    /// </summary>
    public ITextElement Clone(bool deep = true) => new TextImage(Id, AltText, SourceUrl);
}
