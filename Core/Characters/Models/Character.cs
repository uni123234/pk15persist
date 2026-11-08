namespace Project.Core.Characters.Models;

/// <summary>
/// Domain model for an active character participating in gameplay, combat calculations, and turn cycles.
/// Incorporates Health, Power, Armor (flat defense), Resistance (percentage mitigation), Speed, and Status Effects.
/// </summary>
public class Character
{
    public int Id { get; }
    public string Name { get; set; }
    public int BaseHealth { get; }
    public int BasePower { get; }
    public double BaseResistance { get; }
    public int BaseArmor { get; }
    public int BaseSpeed { get; }
    public double BaseResistPenetration { get; }

    public int CurrentHealth { get; private set; }
    public List<Item> ActiveItems { get; } = [];
    public List<Ability> AvailableAbilities { get; } = [];
    public List<StatusEffect> StatusEffects { get; } = [];

    public int Health => Math.Max(0, CurrentHealth);
    public int MaxHealth => (int)Math.Round(BaseHealth + ActiveItems.Sum(i => i.HpMod));
    public int Power => Math.Max(1, (int)Math.Round(BasePower + ActiveItems.Sum(i => i.PowerMod) + StatusEffects.Sum(s => s.PowerBonus)));
    public int Armor => Math.Max(0, (int)Math.Round(BaseArmor + ActiveItems.Sum(i => i.ArmorMod) + StatusEffects.Sum(s => s.ArmorBonus)));
    public int Speed => Math.Max(1, (int)Math.Round(BaseSpeed + ActiveItems.Sum(i => i.SpeedMod) + StatusEffects.Sum(s => s.SpeedBonus)));
    public double Resistance => Math.Clamp(BaseResistance + StatusEffects.Sum(s => s.ResistBonus), 0.0, 0.95);
    public double ResistPenetration => Math.Clamp(BaseResistPenetration, 0.0, 1.0);

    public bool IsAlive => CurrentHealth > 0;
    public bool IsStunned => StatusEffects.Any(s => s.Type == StatusEffectType.Stun);

    public Character(
        int id,
        string name,
        int health,
        int power,
        double resistance,
        int armor = 0,
        int speed = 100,
        double resistPenetration = 0.0)
    {
        Id = id;
        Name = name;
        BaseHealth = health;
        BasePower = power;
        BaseResistance = resistance;
        BaseArmor = armor;
        BaseSpeed = speed;
        BaseResistPenetration = resistPenetration;
        CurrentHealth = health;
    }

    public void AddItem(Item item)
    {
        ActiveItems.Add(item);
    }

    public bool RemoveItem(int itemId)
    {
        return ActiveItems.RemoveAll(i => i.Id == itemId) > 0;
    }

    public void AddAbility(Ability ability)
    {
        AvailableAbilities.Add(ability);
    }

    public bool RemoveAbility(int abilityId)
    {
        return AvailableAbilities.RemoveAll(a => a.Id == abilityId) > 0;
    }

    public void ApplyStatusEffect(StatusEffect effect)
    {
        StatusEffects.Add(effect);
    }

    public int TickStatusEffects()
    {
        var totalDotDamage = 0;
        for (var i = StatusEffects.Count - 1; i >= 0; i--)
        {
            var effect = StatusEffects[i];
            if (effect.Type == StatusEffectType.DamageOverTime && effect.DotDamage > 0)
            {
                totalDotDamage += effect.DotDamage;
                CurrentHealth = Math.Max(0, CurrentHealth - effect.DotDamage);
            }

            effect.DurationTurns--;
            if (effect.DurationTurns <= 0)
            {
                StatusEffects.RemoveAt(i);
            }
        }
        return totalDotDamage;
    }

    public int TakeDamage(int rawDamage)
    {
        var effective = (int)Math.Round(rawDamage * (1.0 - Resistance));
        effective = Math.Max(1, effective);
        CurrentHealth = Math.Max(0, CurrentHealth - effective);
        return effective;
    }

    public int ReceiveHealing(int amount)
    {
        var before = CurrentHealth;
        CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        return CurrentHealth - before;
    }

    public void SetCurrentHealth(int health)
    {
        CurrentHealth = Math.Clamp(health, 0, MaxHealth);
    }
}
