using System.Text.Json.Serialization;

namespace Project.Core.Text.Models;

/// <summary>
/// Common interface for structured text elements supporting polymorphic JSON serialization
/// and Prototype cloning for tree duplication.
/// </summary>
[JsonDerivedType(typeof(Heading), typeDiscriminator: "Heading")]
[JsonDerivedType(typeof(Paragraph), typeDiscriminator: "Paragraph")]
[JsonDerivedType(typeof(TextLink), typeDiscriminator: "Link")]
[JsonDerivedType(typeof(TextImage), typeDiscriminator: "Image")]
[JsonDerivedType(typeof(Root), typeDiscriminator: "Root")]
public interface ITextElement : ITextPrototype<ITextElement>
{
    int Id { get; }
    string Name { get; }
}
