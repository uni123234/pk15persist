using Project.Core.Characters.Combat;
using Project.Core.Characters.Models;

namespace Project.Core.Characters.TurnBased;

public record BattleSimulationResult(
    Character Winner,
    Character Loser,
    int TotalRounds,
    int TotalTurns,
    BattleHistory History
);

/// <summary>
/// Core Business Logic Engine governing turn-based combat progression, speed-based action order, and history recording.
/// </summary>
public class TurnBasedBattleEngine
{
    private int _turnCounter = 1;

    public GameConfig Config { get; }
    public CombatCalculator Calculator { get; }
    public BattleHistory History { get; } = new();

    public TurnBasedBattleEngine(GameConfig? config = null, CombatCalculator? calculator = null)
    {
        Config = config ?? new GameConfig();
        Calculator = calculator ?? new CombatCalculator(Config);
    }

    /// <summary>
    /// Computes the turn action timeline for the round based on Speed.
    /// Fast characters (Speed >= 1.5 * slowest) receive an extra action in the round timeline.
    /// </summary>
    public IReadOnlyList<Character> DetermineTurnOrder(IEnumerable<Character> participants)
    {
        var alive = participants.Where(p => p.IsAlive).OrderByDescending(p => p.Speed).ToList();
        if (alive.Count <= 1)
        {
            return alive;
        }

        var order = new List<Character>(alive);

        if (Config.EnableSpeedAdvantage)
        {
            var fastest = alive.First();
            var slowest = alive.Last();
            if (slowest.Speed > 0 && fastest.Speed >= slowest.Speed * 1.5)
            {
                // Extra action slot granted to fastest combatant
                order.Add(fastest);
            }
        }

        return order;
    }

    public Turn ExecuteTurn(
        int roundNumber,
        Character actor,
        Character target,
        TurnActionType action,
        Ability? ability = null,
        bool isCritical = false)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(target);

        // 1. Process active status effects (e.g. DoT damage)
        var dotTaken = actor.TickStatusEffects();
        var dotLog = dotTaken > 0 ? $" ({actor.Name} took {dotTaken} DoT damage)" : "";

        // 2. Check if actor is stunned
        if (actor.IsStunned)
        {
            var stunTurn = new Turn(
                _turnCounter++,
                roundNumber,
                actor,
                target,
                TurnActionType.SkipStunned,
                0,
                0,
                false,
                $"{actor.Name} is STUNNED and skips action!{dotLog}"
            );
            History.Record(stunTurn);
            return stunTurn;
        }

        if (!actor.IsAlive)
        {
            var deadTurn = new Turn(_turnCounter++, roundNumber, actor, target, TurnActionType.SkipStunned, 0, 0, false, $"{actor.Name} was defeated before acting.");
            History.Record(deadTurn);
            return deadTurn;
        }

        int damage = 0;
        int healing = 0;
        string message;

        switch (action)
        {
            case TurnActionType.PhysicalAttack:
            {
                damage = Calculator.CalculatePhysicalDamage(actor, target, isCritical);
                target.TakeDamage(damage);
                var critMarker = isCritical ? " [CRITICAL! 💥]" : "";
                message = $"{actor.Name} executes physical strike for {damage} damage{critMarker}! (Target HP: {target.Health}/{target.MaxHealth}){dotLog}";
                break;
            }

            case TurnActionType.MagicAbility:
            {
                if (ability != null && ability.Damage > 0)
                {
                    damage = Calculator.CalculateMagicDamage(actor, target, ability);
                    target.TakeDamage(damage);

                    if (ability.AppliedEffect != null)
                    {
                        target.ApplyStatusEffect(ability.AppliedEffect);
                    }

                    message = $"{actor.Name} casts [{ability.Name}] dealing {damage} magic damage! (Target HP: {target.Health}/{target.MaxHealth}){dotLog}";
                }
                else
                {
                    damage = Calculator.CalculatePhysicalDamage(actor, target, isCritical);
                    target.TakeDamage(damage);
                    message = $"{actor.Name} attacks for {damage} damage.{dotLog}";
                }
                break;
            }

            case TurnActionType.Healing:
            {
                var healAmount = ability?.Heal > 0 ? (int)ability.Heal : (int)Math.Round(actor.Power * 0.75);
                healing = target.ReceiveHealing(healAmount);
                message = $"{actor.Name} heals {target.Name} for +{healing} HP! (HP: {target.Health}/{target.MaxHealth}){dotLog}";
                break;
            }

            case TurnActionType.ApplyBuff:
            {
                var buff = new StatusEffect("Heroism", StatusEffectType.Buff, durationTurns: 3, powerBonus: 15, armorBonus: 10);
                actor.ApplyStatusEffect(buff);
                message = $"{actor.Name} activates Heroism (+15 PWR, +10 ARM for 3 turns)!{dotLog}";
                break;
            }

            default:
            {
                message = $"{actor.Name} waits.{dotLog}";
                break;
            }
        }

        var executedTurn = new Turn(_turnCounter++, roundNumber, actor, target, action, damage, healing, isCritical, message);
        History.Record(executedTurn);
        return executedTurn;
    }

    /// <summary>
    /// Executes a complete turn-based duel between two fighters until one is defeated or MaxRounds is reached.
    /// </summary>
    public BattleSimulationResult RunBattle(Character f1, Character f2, Action<Turn>? onTurn = null)
    {
        History.Clear();
        _turnCounter = 1;
        var round = 1;

        while (f1.IsAlive && f2.IsAlive && round <= Config.MaxRounds)
        {
            var timeline = DetermineTurnOrder([f1, f2]);

            foreach (var actor in timeline)
            {
                if (!f1.IsAlive || !f2.IsAlive)
                {
                    break;
                }

                var target = actor == f1 ? f2 : f1;
                var ability = actor.AvailableAbilities.FirstOrDefault(a => a.Damage > 0);
                var action = ability != null ? TurnActionType.MagicAbility : TurnActionType.PhysicalAttack;

                var isCrit = (actor.Power % 3 == 0); // Deterministic critical condition
                var turn = ExecuteTurn(round, actor, target, action, ability, isCrit);
                onTurn?.Invoke(turn);
            }

            round++;
        }

        var winner = f1.IsAlive ? f1 : f2;
        var loser = f1.IsAlive ? f2 : f1;

        return new BattleSimulationResult(winner, loser, round - 1, History.TotalTurns, History);
    }
}
