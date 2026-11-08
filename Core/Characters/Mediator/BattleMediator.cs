using Project.Core.Characters.CombatPipeline;
using Project.Core.Characters.Models;
using Project.Core.Characters.Observer;
using Project.Core.Characters.Strategies;

namespace Project.Core.Characters.Mediator;

/// <summary>
/// [Behavioral Pattern: Mediator - Interface]
/// Declares the contract for coordinating interactions between combatants,
/// decoupled from concrete character implementations.
/// </summary>
public interface IBattleMediator
{
    void RegisterCharacter(Character character);
    void ClearCharacters();
    void ExecuteAttack(Character attacker, Character target, IAttackStrategy? strategy = null);
    void ExecuteAbility(Character caster, Character target, Ability ability);
    void SubscribeObserver(ICombatObserver observer);
    void UnsubscribeObserver(ICombatObserver observer);
}

/// <summary>
/// [Behavioral Pattern: Mediator - Concrete Mediator]
/// Central coordinator managing battle interactions:
/// 1. Uses Strategy to compute attack parameters.
/// 2. Uses Chain of Responsibility to process damage mitigation.
/// 3. Mutates target character state safely.
/// 4. Notifies all Observers about combat events.
/// </summary>
public class BattleMediator : IBattleMediator
{
    private readonly List<Character> _participants = [];
    private readonly List<ICombatObserver> _observers = [];
    private readonly IDamageStepHandler _damagePipeline;
    private readonly IAttackStrategy _defaultStrategy = new StandardAttackStrategy();

    public BattleMediator()
    {
        // Build Chain of Responsibility: Critical -> Resistance -> Minimum Clamp
        var crit = new CriticalMultiplierHandler();
        var resist = new ResistanceMitigationHandler();
        var clamp = new MinimumDamageClampHandler();

        crit.SetNext(resist).SetNext(clamp);
        _damagePipeline = crit;
    }

    public void RegisterCharacter(Character character)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (!_participants.Contains(character))
        {
            _participants.Add(character);
        }
    }

    public void SubscribeObserver(ICombatObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
        }
    }

    public void UnsubscribeObserver(ICombatObserver observer)
    {
        _observers.Remove(observer);
    }

    public void ExecuteAttack(Character attacker, Character target, IAttackStrategy? strategy = null)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(target);

        if (!attacker.IsAlive || !target.IsAlive)
        {
            return;
        }

        // 1. Prepare context via Strategy pattern
        var activeStrategy = strategy ?? _defaultStrategy;
        var context = activeStrategy.PrepareAttackContext(attacker, target);

        // 2. Process damage via Chain of Responsibility
        _damagePipeline.Handle(context);

        // 3. Apply state change to target
        var targetHealthBefore = target.CurrentHealth;
        target.SetCurrentHealth(Math.Max(0, targetHealthBefore - context.CurrentDamage));

        // 4. Notify Observers (Observer pattern)
        var damageEvent = new DamageDealtEvent(
            attacker.Name,
            target.Name,
            context.CurrentDamage,
            target.Health,
            context.IsCritical
        );

        foreach (var observer in _observers)
        {
            observer.OnDamageDealt(damageEvent);
        }

        // Check lethal condition
        if (!target.IsAlive && targetHealthBefore > 0)
        {
            var defeatEvent = new CharacterDefeatedEvent(attacker.Name, target.Name);
            foreach (var observer in _observers)
            {
                observer.OnCharacterDefeated(defeatEvent);
            }
        }
    }

    public void ClearCharacters() => _participants.Clear();

    public void ExecuteAbility(Character caster, Character target, Ability ability)
    {
        ArgumentNullException.ThrowIfNull(caster);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(ability);

        if (!caster.IsAlive)
        {
            return;
        }

        // Notify ability cast
        var abilityEvent = new AbilityUsedEvent(caster.Name, ability.Name, target.Name, ability.Damage, ability.Heal);
        foreach (var observer in _observers)
        {
            observer.OnAbilityUsed(abilityEvent);
        }

        // If offensive ability
        if (ability.Damage > 0)
        {
            var rawDmg = (int)Math.Round(caster.Power + ability.Damage);
            var context = new DamageContext(caster, target, rawDmg, isCritical: false);
            _damagePipeline.Handle(context);

            var targetHealthBefore = target.CurrentHealth;
            target.SetCurrentHealth(Math.Max(0, targetHealthBefore - context.CurrentDamage));

            var damageEvent = new DamageDealtEvent(caster.Name, target.Name, context.CurrentDamage, target.Health, false);
            foreach (var observer in _observers)
            {
                observer.OnDamageDealt(damageEvent);
            }

            if (!target.IsAlive && targetHealthBefore > 0)
            {
                var defeatEvent = new CharacterDefeatedEvent(caster.Name, target.Name);
                foreach (var observer in _observers)
                {
                    observer.OnCharacterDefeated(defeatEvent);
                }
            }
        }

        // If healing ability
        if (ability.Heal > 0)
        {
            var healAmount = (int)Math.Round(ability.Heal);
            var actualHealed = target.ReceiveHealing(healAmount);

            var healEvent = new HealingReceivedEvent(target.Name, actualHealed, target.Health, target.MaxHealth);
            foreach (var observer in _observers)
            {
                observer.OnHealingReceived(healEvent);
            }
        }
    }
}
