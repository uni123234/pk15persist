namespace Project.Infra.Data.Models;

/// <summary>
/// Serializable data entity for status effects attached to a character.
/// </summary>
public record StatusEffectData(
    string Name,
    string Type,
    int DurationTurns,
    int PowerBonus = 0,
    int ArmorBonus = 0,
    int SpeedBonus = 0,
    double ResistBonus = 0.0,
    int DotDamage = 0
);

/// <summary>
/// Data entity representing a character record in persistence storage.
/// Backward compatible with legacy 4-tuple parameters while storing extended attributes.
/// </summary>
public record CharacterData(
    int Id,
    string Name,
    double Hp,
    double Power,
    double Armor = 0,
    double Speed = 100,
    double Resistance = 0.1,
    double ResistPenetration = 0.0,
    List<StatusEffectData>? StatusEffects = null
);

/// <summary>
/// Data entity representing an item record in persistence storage.
/// Backward compatible with legacy (Id, Name, HpMod, PowerMod) while storing ArmorMod and SpeedMod.
/// </summary>
public record ItemData(
    int Id,
    string Name,
    double HpMod,
    double PowerMod,
    double ArmorMod = 0,
    double SpeedMod = 0
);

/// <summary>
/// Data entity representing the many-to-many relationship between characters and items.
/// </summary>
public record CharacterItems(
    int Id,
    int CharacterId,
    int ItemId
);

/// <summary>
/// Data entity representing an ability record in persistence storage.
/// Backward compatible with legacy fields while storing magic and scaling parameters.
/// </summary>
public record AbilityData(
    int Id,
    string Name,
    double Damage,
    double Heal,
    string Description,
    bool IsMagic = false,
    double ResistPenetration = 0.0,
    double BonusProficiency = 0.0
);

/// <summary>
/// Data entity representing the many-to-many relationship between characters and abilities.
/// </summary>
public record CharacterAbilities(
    int Id,
    int CharacterId,
    int AbilityId
);

/// <summary>
/// Data entity representing a node in the structured text document tree.
/// </summary>
public record TextNodeData(
    int Id,
    int? ParentId,
    string NodeType,
    string Name,
    string Content,
    int Rank,
    string? Extra = null
);
