using Project.Core.Characters.Models;

namespace Project.Core.Characters.TurnBased;

public enum TurnActionType
{
    PhysicalAttack,
    MagicAbility,
    Healing,
    ApplyBuff,
    SkipStunned
}

/// <summary>
/// Domain model encapsulating an individual step/turn within a combat round.
/// Preserves complete historical context (Actor, Target, Action, Damage, Log).
/// </summary>
public class Turn
{
    public int TurnNumber { get; }
    public int RoundNumber { get; }
    public Character Actor { get; }
    public Character Target { get; }
    public TurnActionType ActionType { get; }
    public int DamageDealt { get; }
    public int HealingDone { get; }
    public bool IsCritical { get; }
    public string LogMessage { get; }
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    public Turn(
        int turnNumber,
        int roundNumber,
        Character actor,
        Character target,
        TurnActionType actionType,
        int damageDealt,
        int healingDone,
        bool isCritical,
        string logMessage)
    {
        TurnNumber = turnNumber;
        RoundNumber = roundNumber;
        Actor = actor;
        Target = target;
        ActionType = actionType;
        DamageDealt = damageDealt;
        HealingDone = healingDone;
        IsCritical = isCritical;
        LogMessage = logMessage;
    }
}
