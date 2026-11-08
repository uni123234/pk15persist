using System.Text.Json.Serialization;

namespace Project.Core.Text.Models;

/// <summary>
/// Root document node containing top-level sections and text elements.
/// Supports Prototype cloning of the complete document hierarchy.
/// </summary>
public record Root : Heading
{
    [JsonConstructor]
    public Root(int id, string name, int rank, List<ITextElement> children, int? parentId = null)
        : base(id, name, rank, children, parentId)
    {
    }

    public Root(string title = "Document") : base(1, title, 0, [], null)
    {
    }

    public override ITextElement Clone(bool deep = true)
    {
        var clonedChildren = deep
            ? Children.Select(child => child.Clone(deep: true)).ToList()
            : [];

        return new Root(Id, Name, Rank, clonedChildren, ParentId);
    }
}
