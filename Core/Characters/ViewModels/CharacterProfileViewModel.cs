using Project.Core.Characters.Models;
using Project.Infra.Remote.Models;

namespace Project.Core.Characters.ViewModels;

/// <summary>
/// Presentation ViewModel formatting character domain data for GUI and CLI display.
/// Integrates API metadata, visual avatar links, and aggregated combat statistics.
/// </summary>
public class CharacterProfileViewModel
{
    public int Id { get; }
    public string Name { get; }
    public string Element { get; }
    public string WeaponType { get; }
    public string Nation { get; }
    public string RarityStars { get; }
    public string AvatarUrl { get; }
    public string? LocalPortraitPath { get; set; }
    public bool IsPortraitCached => !string.IsNullOrEmpty(LocalPortraitPath) && File.Exists(LocalPortraitPath);

    public int CurrentHealth { get; }
    public int MaxHealth { get; }
    public int Power { get; }
    public int Armor { get; }
    public double Resistance { get; }
    public int Speed { get; }
    public bool IsAlive { get; }

    public IReadOnlyList<string> EquippedItemNames { get; }
    public IReadOnlyList<string> LearnedAbilityNames { get; }

    public CharacterProfileViewModel(Character character, RemoteCharacterData? remoteData = null, string? localPortraitPath = null)
    {
        ArgumentNullException.ThrowIfNull(character);

        Id = character.Id;
        Name = character.Name;
        Element = remoteData?.Vision ?? "Neutral";
        WeaponType = remoteData?.Weapon ?? "Sword";
        Nation = remoteData?.Nation ?? "Unknown";

        var stars = remoteData?.Rarity ?? 4;
        RarityStars = new string('★', Math.Clamp(stars, 1, 5));

        var cleanName = character.Name.ToLowerInvariant().Replace(' ', '-');
        AvatarUrl = $"https://genshin.jmp.blue/characters/{cleanName}/icon";
        LocalPortraitPath = localPortraitPath;

        CurrentHealth = character.Health;
        MaxHealth = character.MaxHealth;
        Power = character.Power;
        Armor = character.Armor;
        Resistance = character.Resistance;
        Speed = character.Speed;
        IsAlive = character.IsAlive;

        EquippedItemNames = character.ActiveItems.Select(i => i.Name).ToList().AsReadOnly();
        LearnedAbilityNames = character.AvailableAbilities.Select(a => a.Name).ToList().AsReadOnly();
    }
}
