using Project.Core.Characters.CombatPipeline;
using Project.Core.Characters.Models;

namespace Project.Core.Characters.Strategies;

/// <summary>
/// [Behavioral Pattern: Strategy]
/// Strategy interface defining algorithmic variants for executing character attacks.
/// </summary>
public interface IAttackStrategy
{
    string StrategyName { get; }
    DamageContext PrepareAttackContext(Character attacker, Character target);
}

/// <summary>
/// Default standard physical attack based on character's base power and equipped items.
/// </summary>
public class StandardAttackStrategy : IAttackStrategy
{
    public string StrategyName => "Standard Physical Strike";

    public DamageContext PrepareAttackContext(Character attacker, Character target)
    {
        return new DamageContext(attacker, target, attacker.Power, isCritical: false);
    }
}

/// <summary>
/// High-risk critical strike strategy amplifying damage output.
/// </summary>
public class CriticalStrikeStrategy : IAttackStrategy
{
    public string StrategyName => "Focused Critical Strike";

    public DamageContext PrepareAttackContext(Character attacker, Character target)
    {
        return new DamageContext(attacker, target, attacker.Power, isCritical: true);
    }
}

/// <summary>
/// Armor-piercing strike that artificially suppresses target resistance during damage resolution.
/// </summary>
public class ArmorPiercingStrategy : IAttackStrategy
{
    public string StrategyName => "Armor Piercing Strike";

    public DamageContext PrepareAttackContext(Character attacker, Character target)
    {
        // Amplified baseline bypassing normal defense calculations
        var baseDmg = (int)Math.Round(attacker.Power * 1.2);
        return new DamageContext(attacker, target, baseDmg, isCritical: false);
    }
}
