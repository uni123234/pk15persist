using Project.Infra.Data.Models;

namespace Project.Infra.Data.Serialization;

/// <summary>
/// Persistence container holding game state: characters, items, and abilities.
/// </summary>
public class CharacterSave : SaveBase
{
    public List<CharacterData>? Characters { get; set; } = [];
    public List<AbilityData>? Abilities { get; set; } = [];
    public List<ItemData>? Items { get; set; } = [];
    public List<CharacterItems>? CharacterItems { get; set; } = [];
    public List<CharacterAbilities>? CharacterAbilities { get; set; } = [];
}
