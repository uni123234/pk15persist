using Project.CLI.Rendering;

namespace Project.Core.Characters.TurnBased;

/// <summary>
/// Domain model encapsulating the sequential history of turns executed in the battle engine.
/// Allows querying, inspection, and tabular reporting.
/// </summary>
public class BattleHistory
{
    private readonly List<Turn> _turns = [];

    public IReadOnlyList<Turn> Turns => _turns.AsReadOnly();
    public int TotalTurns => _turns.Count;

    public void Record(Turn turn)
    {
        ArgumentNullException.ThrowIfNull(turn);
        _turns.Add(turn);
    }

    public void Clear()
    {
        _turns.Clear();
    }

    public string FormatHistoryTable()
    {
        if (_turns.Count == 0)
        {
            return "  (No turns recorded in battle history)";
        }

        var headers = new[] { "Turn", "Round", "Actor", "Action", "Target", "Dmg/Heal", "Target HP", "Summary" };
        var rows = new List<IReadOnlyList<string>>();

        foreach (var t in _turns)
        {
            var value = t.DamageDealt > 0 ? $"-{t.DamageDealt} HP"
                      : t.HealingDone > 0 ? $"+{t.HealingDone} HP"
                      : "0";
            var crit = t.IsCritical ? " [CRIT]" : "";
            rows.Add([
                $"#{t.TurnNumber}",
                $"R{t.RoundNumber}",
                t.Actor.Name,
                t.ActionType.ToString() + crit,
                t.Target.Name,
                value,
                $"{t.Target.Health}/{t.Target.MaxHealth}",
                t.LogMessage
            ]);
        }

        return CLIGraphic.FormatTable(headers, rows);
    }
}
