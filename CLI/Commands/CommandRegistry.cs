namespace Project.CLI.Commands;

/// <summary>
/// Dynamic command registry managing registration and dispatch of CLI commands via selector-action delegates.
/// Adheres to Open-Closed Principle (OCP) by permitting seamless registration without modifying core dispatch loop.
/// </summary>
public class CommandRegistry
{
    private readonly Dictionary<string, ICommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ICommand command)
    {
        _commands[command.Name] = command;
    }

    public void Register(string name, string description, string usage, Action<ParsedCommand, CommandContext> action)
    {
        _commands[name] = new DelegateCommand(name, description, usage, action);
    }

    public bool Execute(ParsedCommand cmd, CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(cmd.Name))
        {
            return true;
        }

        if (_commands.TryGetValue(cmd.Name, out var command))
        {
            try
            {
                command.Execute(cmd, context);
            }
            catch (Exception ex)
            {
                context.Displayer.Display($"Error executing '{cmd.Name}': {ex.Message}");
            }
            return true;
        }

        context.Displayer.Display($"Unknown command '{cmd.Name}'. Type 'help' to see available commands.");
        return false;
    }

    public IEnumerable<ICommand> GetAllCommands() => _commands.Values;

    private class DelegateCommand(string name, string description, string usage, Action<ParsedCommand, CommandContext> action) : ICommand
    {
        public string Name { get; } = name;
        public string Description { get; } = description;
        public string Usage { get; } = usage;

        public void Execute(ParsedCommand command, CommandContext context) => action(command, context);
    }
}
