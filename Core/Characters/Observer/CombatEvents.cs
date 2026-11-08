namespace Project.Core.Characters.Observer;

public record DamageDealtEvent(string AttackerName, string TargetName, int DamageDealt, int TargetRemainingHealth, bool IsCritical);

public record HealingReceivedEvent(string TargetName, int AmountHealed, int TargetCurrentHealth, int TargetMaxHealth);

public record CharacterDefeatedEvent(string KillerName, string DefeatedName);

public record AbilityUsedEvent(string CasterName, string AbilityName, string TargetName, double Damage, double Heal);

/// <summary>
/// [Behavioral Pattern: Observer]
/// Observer contract receiving decoupled notifications about combat events.
/// </summary>
public interface ICombatObserver
{
    void OnDamageDealt(DamageDealtEvent e);
    void OnHealingReceived(HealingReceivedEvent e);
    void OnCharacterDefeated(CharacterDefeatedEvent e);
    void OnAbilityUsed(AbilityUsedEvent e);
}
