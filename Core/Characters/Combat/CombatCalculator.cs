using Project.Core.Characters.Models;

namespace Project.Core.Characters.Combat;

/// <summary>
/// Domain service implementing exact mathematical combat interaction formulas:
/// 1. Physical Attack with flat Armor mitigation and percentage Resistance.
/// 2. Magic Attack bypassing flat Armor with Resistance Penetration and Proficiency Multipliers.
/// </summary>
public class CombatCalculator
{
    private readonly GameConfig _config;

    public CombatCalculator(GameConfig? config = null)
    {
        _config = config ?? new GameConfig();
    }

    /// <summary>
    /// Calculates physical attack damage:
    /// dmg_phys = max(0, dmg_flat - armor_tgt) * max(0, 1 - max(0, res_tgt - res_pen))
    /// </summary>
    public int CalculatePhysicalDamage(Character attacker, Character target, bool isCritical = false)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(target);

        var baseDmg = (double)attacker.Power;
        if (isCritical)
        {
            baseDmg *= _config.CriticalMultiplier;
        }

        // Flat armor deduction
        var afterArmor = Math.Max(0.0, baseDmg - target.Armor);
        if (afterArmor <= 0.0)
        {
            return 0; // Completely absorbed by armor
        }

        // Net resistance after penetration
        var netResist = Math.Clamp(target.Resistance - attacker.ResistPenetration, 0.0, 0.95);
        var effective = afterArmor * (1.0 - netResist);

        return Math.Max(1, (int)Math.Round(effective));
    }

    /// <summary>
    /// Calculates magic ability damage using the exact formula from the assignment:
    /// dmg_magic = (dmg_flat * max(0, 1 - res_tgt + res_pen) + bonus_flat) * max(0.1, 1 + bonus_prof - debuff_ability)
    /// where bonus_flat = sum(bonus_item + bonus_skill + bonus_buff)
    /// </summary>
    public int CalculateMagicDamage(Character attacker, Character target, Ability ability)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(ability);

        var dmgFlat = ability.Damage;
        var resTgt = target.Resistance;
        var resPen = ability.ResistPenetration + attacker.ResistPenetration;

        // Magic bypasses flat armor; resistance factor: max(0, 1 - res_tgt + res_pen)
        var resistFactor = Math.Max(0.0, 1.0 - resTgt + resPen);

        // Sum of item, skill, and buff power bonuses
        var bonusFlat = attacker.ActiveItems.Sum(i => i.PowerMod)
                      + attacker.StatusEffects.Where(s => s.Type == StatusEffectType.Buff).Sum(s => s.PowerBonus);

        // Proficiency bonus from ability minus debuffs on attacker
        var bonusProf = ability.BonusProficiency;
        var debuffFactor = attacker.StatusEffects.Where(s => s.Type == StatusEffectType.Debuff).Sum(s => 0.10);
        var profMultiplier = Math.Max(0.1, 1.0 + bonusProf - debuffFactor);

        var baseCalc = (dmgFlat * resistFactor) + bonusFlat;
        var finalDmg = baseCalc * profMultiplier;

        return Math.Max(1, (int)Math.Round(finalDmg));
    }
}
