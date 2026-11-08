using Project.CLI.Core;

namespace Project.CLI.Strategies;

/// <summary>
/// Strategy interface grouping a cohesive set of CLI commands (Information, Transformative, Infrastructure).
/// Enables dynamic parameterization and extension of the CLI facade.
/// </summary>
public interface ICommandStrategy
{
    string StrategyName { get; }
    string Category { get; }
    string Description { get; }

    IReadOnlyList<Command> GetCommands();
}
