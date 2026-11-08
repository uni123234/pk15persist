namespace Project.Core.Characters.Models;

/// <summary>
/// Domain model for an active ability that can deal physical or magical damage, apply healing, or grant buffs.
/// </summary>
public class Ability
{
    public int Id { get; }
    public string Name { get; }
    public double Damage { get; }
    public double Heal { get; }
    public string Description { get; }
    public bool IsMagic { get; }
    public double ResistPenetration { get; }
    public double BonusProficiency { get; }
    public StatusEffect? AppliedEffect { get; }

    public Ability(
        int id,
        string name,
        double damage,
        double heal,
        string description,
        bool isMagic = true,
        double resistPenetration = 0.0,
        double bonusProficiency = 0.0,
        StatusEffect? appliedEffect = null)
    {
        Id = id;
        Name = name;
        Damage = damage;
        Heal = heal;
        Description = description;
        IsMagic = isMagic;
        ResistPenetration = resistPenetration;
        BonusProficiency = bonusProficiency;
        AppliedEffect = appliedEffect;
    }
}
