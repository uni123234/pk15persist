using Project.Core.Characters.Models;
using Project.Infra.Remote.Models;

namespace Project.Infra.Remote;

/// <summary>
/// Domain adapter transforming remote API payloads into internal domain models (Character, Item, Ability).
/// Adheres to the Single Responsibility Principle and bounds external contract changes.
/// </summary>
public static class RemoteDataMapper
{
    /// <summary>
    /// Maps remote character data to a domain Character entity with derived RPG combat statistics.
    /// </summary>
    public static Character ToCharacter(RemoteCharacterData remote, int id)
    {
        ArgumentNullException.ThrowIfNull(remote);

        // Derive stats from character rarity and vision
        var rarity = Math.Clamp(remote.Rarity, 1, 5);
        var baseHealth = 100 + (rarity * 15);
        var basePower = 20 + (rarity * 4);
        var baseResistance = Math.Round(0.04 * rarity, 2);

        var character = new Character(id, remote.Name, baseHealth, basePower, baseResistance);

        // Map remote skill talents to domain abilities
        if (remote.SkillTalents != null)
        {
            var abilityId = 1;
            foreach (var talent in remote.SkillTalents)
            {
                var powerModifier = talent.Type switch
                {
                    "Elemental Burst" => 45 + (rarity * 6),
                    "Elemental Skill" => 25 + (rarity * 3),
                    _ => 15 + rarity
                };

                var ability = new Ability(
                    abilityId++,
                    talent.Name,
                    powerModifier,
                    healthCost: 0,
                    talent.Description ?? string.Empty
                );

                character.AddAbility(ability);
            }
        }

        return character;
    }

    /// <summary>
    /// Maps remote weapon data to a domain Item entity.
    /// </summary>
    public static Item ToItem(RemoteWeaponData remote, int id)
    {
        ArgumentNullException.ThrowIfNull(remote);

        var rarity = Math.Clamp(remote.Rarity, 1, 5);
        var powerBonus = remote.BaseAttack > 0 ? (int)(remote.BaseAttack * 0.08) : (rarity * 6);
        var healthBonus = rarity * 8;

        return new Item(id, remote.Name, healthBonus, powerBonus);
    }
}
