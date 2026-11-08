namespace Project.Core.Text.Models;

/// <summary>
/// [Creational Pattern: Prototype]
/// Defines a contract for cloning elements within the tree structure.
/// Supports both shallow copy and deep recursive cloning of hierarchical trees.
/// </summary>
public interface ITextPrototype<out T>
{
    /// <summary>
    /// Clones the current node. If deep is true, recursively clones all child elements.
    /// </summary>
    T Clone(bool deep = true);
}
