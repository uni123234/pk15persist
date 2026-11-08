using System.Text.Json.Serialization;

namespace Project.Core.Text.Models;

/// <summary>
/// Composite model representing a section heading containing child elements.
/// Implements Prototype pattern for shallow and recursive deep cloning of tree branches.
/// </summary>
public record Heading : ITextElement
{
    public int Id { get; init; }
    public string Name { get; init; }
    public int Rank { get; init; }
    public List<ITextElement> Children { get; init; } = [];
    public int? ParentId { get; init; }

    [JsonConstructor]
    public Heading(int id, string name, int rank, List<ITextElement>? children = null, int? parentId = null)
    {
        Id = id;
        Name = name;
        Rank = rank;
        Children = children ?? [];
        ParentId = parentId;
    }

    public Heading(string name, List<ITextElement> children) : this(0, name, 0, children) { }

    public void AddChild(ITextElement element) => Children.Add(element);

    public bool RemoveChild(int elementId) => Children.RemoveAll(c => c.Id == elementId) > 0;

    /// <summary>
    /// [Prototype Pattern] Clones the heading node.
    /// If deep is true, recursively clones the entire subtree.
    /// </summary>
    public virtual ITextElement Clone(bool deep = true)
    {
        List<ITextElement> clonedChildren;
        if (deep)
        {
            clonedChildren = Children.Select(child => child.Clone(deep: true)).ToList();
        }
        else
        {
            clonedChildren = [];
        }

        return new Heading(Id, Name, Rank, clonedChildren, ParentId);
    }
}
