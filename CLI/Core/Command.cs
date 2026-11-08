namespace Project.CLI.Core;

/// <summary>
/// Object-oriented abstraction for a terminal command.
/// Encapsulates metadata, validation, and execution logic.
/// </summary>
public abstract class Command
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Usage { get; }
    public abstract string Category { get; }
    public virtual IReadOnlyList<string> Aliases => [];

    public abstract CommandResult Execute(Args args, CommandExecutionContext context);
}

/// <summary>
/// Delegate-based command adapter for quick or lightweight command definitions.
/// </summary>
public class DelegateCommand(
    string name,
    string description,
    string usage,
    string category,
    Func<Args, CommandExecutionContext, CommandResult> handler,
    IEnumerable<string>? aliases = null) : Command
{
    public override string Name => name;
    public override string Description => description;
    public override string Usage => usage;
    public override string Category => category;
    public override IReadOnlyList<string> Aliases { get; } = aliases?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)[];

    public override CommandResult Execute(Args args, CommandExecutionContext context)
    {
        return handler(args, context);
    }
}
