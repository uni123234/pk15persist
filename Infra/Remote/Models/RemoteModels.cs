using System.Text.Json.Serialization;

namespace Project.Infra.Remote.Models;

/// <summary>
/// Data Transfer Object representing character payload received from the remote REST API.
/// Compatible with https://genshin.jmp.blue/characters/{name}
/// </summary>
public class RemoteCharacterData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("vision")]
    public string? Vision { get; set; }

    [JsonPropertyName("weapon")]
    public string? Weapon { get; set; }

    [JsonPropertyName("rarity")]
    public int Rarity { get; set; } = 4;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("skillTalents")]
    public List<RemoteSkillTalent>? SkillTalents { get; set; } = [];
}

/// <summary>
/// Data Transfer Object representing active skills and talents of a character.
/// </summary>
public class RemoteSkillTalent
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("unlock")]
    public string? Unlock { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

/// <summary>
/// Data Transfer Object representing weapon payload received from the remote REST API.
/// Compatible with https://genshin.jmp.blue/weapons/{name}
/// </summary>
public class RemoteWeaponData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("rarity")]
    public int Rarity { get; set; } = 4;

    [JsonPropertyName("baseAttack")]
    public int BaseAttack { get; set; } = 42;

    [JsonPropertyName("subStat")]
    public string? SubStat { get; set; }

    [JsonPropertyName("passiveName")]
    public string? PassiveName { get; set; }

    [JsonPropertyName("passiveDesc")]
    public string? PassiveDesc { get; set; }
}
