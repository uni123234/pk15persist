using Project.Infra.Display;

namespace Project.Core.Characters.Observer;

/// <summary>
/// Concrete observer formatting combat events for display through IDisplayer.
/// </summary>
public class CombatLogObserver(IDisplayer displayer) : ICombatObserver
{
    private readonly IDisplayer _displayer = displayer;

    public void OnDamageDealt(DamageDealtEvent e)
    {
        var critIndicator = e.IsCritical ? " [CRITICAL STRIKE! 💥]" : "";
        _displayer.Display($"[Combat] {e.AttackerName} attacked {e.TargetName} for {e.DamageDealt} damage{critIndicator}. ({e.TargetName} HP: {e.TargetRemainingHealth})");
    }

    public void OnHealingReceived(HealingReceivedEvent e)
    {
        _displayer.Display($"[Combat] {e.TargetName} received {e.AmountHealed} healing. (HP: {e.TargetCurrentHealth}/{e.TargetMaxHealth})");
    }

    public void OnCharacterDefeated(CharacterDefeatedEvent e)
    {
        _displayer.Display($"[Combat] 💀 {e.DefeatedName} was defeated by {e.KillerName}!");
    }

    public void OnAbilityUsed(AbilityUsedEvent e)
    {
        var desc = e.Damage > 0 && e.Heal > 0 ? $"Dmg: {e.Damage}, Heal: {e.Heal}"
                 : e.Damage > 0 ? $"Dmg: {e.Damage}"
                 : $"Heal: {e.Heal}";
        _displayer.Display($"[Combat] ✨ {e.CasterName} unleashed ability [{e.AbilityName}] on {e.TargetName} ({desc})!");
    }
}

/// <summary>
/// Concrete observer maintaining aggregate combat statistics without mutating game state.
/// </summary>
public class CombatStatsObserver : ICombatObserver
{
    public Dictionary<string, int> DamageDealt { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> DamageTaken { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Kills { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void OnDamageDealt(DamageDealtEvent e)
    {
        DamageDealt[e.AttackerName] = DamageDealt.GetValueOrDefault(e.AttackerName) + e.DamageDealt;
        DamageTaken[e.TargetName] = DamageTaken.GetValueOrDefault(e.TargetName) + e.DamageDealt;
    }

    public void OnHealingReceived(HealingReceivedEvent e) { }

    public void OnCharacterDefeated(CharacterDefeatedEvent e)
    {
        Kills[e.KillerName] = Kills.GetValueOrDefault(e.KillerName) + 1;
    }

    public void OnAbilityUsed(AbilityUsedEvent e) { }
}
