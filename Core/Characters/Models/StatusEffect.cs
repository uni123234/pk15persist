namespace Project.Core.Characters.Models;

public enum StatusEffectType
{
    Buff,
    Debuff,
    DamageOverTime,
    Stun
}

/// <summary>
/// Status effect modifying character attributes or applying turn-based periodic effects.
/// </summary>
public class StatusEffect
{
    public string Name { get; }
    public StatusEffectType Type { get; }
    public int DurationTurns { get; set; }
    public int PowerBonus { get; }
    public int ArmorBonus { get; }
    public int SpeedBonus { get; }
    public double ResistBonus { get; }
    public int DotDamage { get; }

    public StatusEffect(
        string name,
        StatusEffectType type,
        int durationTurns,
        int powerBonus = 0,
        int armorBonus = 0,
        int speedBonus = 0,
        double resistBonus = 0.0,
        int dotDamage = 0)
    {
        Name = name;
        Type = type;
        DurationTurns = Math.Max(1, durationTurns);
        PowerBonus = powerBonus;
        ArmorBonus = armorBonus;
        SpeedBonus = speedBonus;
        ResistBonus = resistBonus;
        DotDamage = dotDamage;
    }
}
