namespace Project.Core.Characters.Models;

/// <summary>
/// Domain configuration governing gameplay constants, combat rules, and turn progression.
/// </summary>
public class GameConfig
{
    public string GameTitle { get; set; } = "RPG Turn-Based Battle Engine";
    public int MaxRounds { get; set; } = 50;
    public double BaseCriticalChance { get; set; } = 0.15;
    public double CriticalMultiplier { get; set; } = 1.75;
    public bool EnableStatusEffects { get; set; } = true;
    public bool EnableSpeedAdvantage { get; set; } = true;
}
