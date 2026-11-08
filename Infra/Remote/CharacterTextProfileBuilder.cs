using Project.Core.Characters.Models;
using Project.Core.Text.Services;
using Project.Infra.Display;
using Project.Infra.Remote.Models;

namespace Project.Infra.Remote;

/// <summary>
/// Constructs structured text document representations (Core.Text composite tree)
/// for characters imported from the remote REST API.
/// Meets Requirement 3 of Practical Work 7.
/// </summary>
public static class CharacterTextProfileBuilder
{
    /// <summary>
    /// Builds a full structured text hierarchy representing the character, their lore,
    /// derived RPG stats, active talents, and equipped armament.
    /// </summary>
    public static TextDocumentManager BuildProfile(
        Character character,
        RemoteCharacterData remoteData,
        IDisplayer displayer,
        RemoteWeaponData? equippedWeaponData = null)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(remoteData);
        ArgumentNullException.ThrowIfNull(displayer);

        var docTitle = $"{character.Name}_Profile";
        var manager = new TextDocumentManager(displayer, docTitle);

        // 1. Overview Section
        manager.AddHeading("1. Overview & Lore", 1);
        manager.Cd("1. Overview & Lore");

        var stars = new string('★', Math.Clamp(remoteData.Rarity, 1, 5));
        var subtitle = string.IsNullOrWhiteSpace(remoteData.Title) ? string.Empty : $" [{remoteData.Title}]";
        manager.AddParagraph($"Character: {remoteData.Name}{subtitle}");
        manager.AddParagraph($"Vision / Element: {remoteData.Vision ?? "Unknown"} | Weapon Class: {remoteData.Weapon ?? "Generic"} | Rarity: {stars}");

        if (!string.IsNullOrWhiteSpace(remoteData.Description))
        {
            manager.AddParagraph($"Biography: {remoteData.Description}");
        }

        manager.AddLink("Official Genshin API Reference", $"https://genshin.jmp.blue/characters/{remoteData.Name.ToLowerInvariant().Replace(' ', '-')}");
        manager.Cd("/");

        // 2. Combat Attributes Section
        manager.AddHeading("2. Combat Attributes", 1);
        manager.Cd("2. Combat Attributes");
        manager.AddParagraph($"Max Health: {character.MaxHealth} HP (Current: {character.Health} HP)");
        manager.AddParagraph($"Base Power: {character.Power} ATK");
        manager.AddParagraph($"Damage Mitigation: {(int)(character.Resistance * 100)}% Physical/Elemental Resistance");
        manager.AddParagraph($"Status: {(character.IsAlive ? "Active & Ready for Combat" : "Incapacitated")}");
        manager.Cd("/");

        // 3. Talents & Abilities Section
        if (character.Abilities.Count > 0)
        {
            manager.AddHeading("3. Skill Talents", 1);
            manager.Cd("3. Skill Talents");

            foreach (var ability in character.Abilities)
            {
                manager.AddHeading(ability.Name, 2);
                manager.Cd(ability.Name);
                manager.AddParagraph($"Type: Active Combat Ability | Power Modifier: +{ability.PowerModifier} DMG");
                if (!string.IsNullOrWhiteSpace(ability.Description))
                {
                    manager.AddParagraph($"Effect: {ability.Description}");
                }
                manager.Up();
            }

            manager.Cd("/");
        }

        // 4. Equipped Armament Section
        if (character.EquippedItems.Count > 0)
        {
            manager.AddHeading("4. Equipped Armament", 1);
            manager.Cd("4. Equipped Armament");

            foreach (var item in character.EquippedItems)
            {
                manager.AddHeading(item.Name, 2);
                manager.Cd(item.Name);
                manager.AddParagraph($"Bonus Health: +{item.HealthBonus} HP | Bonus Attack Power: +{item.PowerBonus} ATK");

                if (equippedWeaponData != null && equippedWeaponData.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase))
                {
                    manager.AddParagraph($"Base Weapon Attack: {equippedWeaponData.BaseAttack} | Sub-stat: {equippedWeaponData.SubStat ?? "N/A"}");
                    if (!string.IsNullOrWhiteSpace(equippedWeaponData.PassiveName))
                    {
                        manager.AddParagraph($"Passive [{equippedWeaponData.PassiveName}]: {equippedWeaponData.PassiveDesc}");
                    }
                }

                manager.Up();
            }

            manager.Cd("/");
        }

        return manager;
    }
}
