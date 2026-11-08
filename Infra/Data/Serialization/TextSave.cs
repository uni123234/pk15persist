using Project.Core.Text.Models;
using Project.Infra.Data.Models;

namespace Project.Infra.Data.Serialization;

/// <summary>
/// Persistence container holding structured text document tree state.
/// </summary>
public class TextSave : SaveBase
{
    public Root? TextRoot { get; set; }
    public List<TextNodeData>? Nodes { get; set; } = [];
}
