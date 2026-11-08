using Project.CLI.Core;
using Project.CLI.Rendering;
using Project.CLI.Strategies;
using Project.Core.Characters.Services;
using Project.Infra.Caching;
using Project.Infra.Data.Repositories;
using Project.Infra.Data.SaveSystem;
using Project.Infra.Remote;

namespace Project.CLI.Facade;

/// <summary>
/// CLI Facade interface as specified by the architecture design document.
/// Provides UseStrategy, ExecCommand, and Display operations.
/// </summary>
public interface ICliFacade
{
    IRenderer Renderer { get; set; }
    IReadOnlyCollection<ICommandStrategy> Strategies { get; }
    IReadOnlyCollection<Command> Commands { get; }

    void UseStrategy(ICommandStrategy strategy);
    CommandResult ExecCommand(string rawInput);
    void Display(IRenderable renderable);
    void Display(string message);
}

/// <summary>
/// Central facade governing the Command-Line Interface subsystem.
/// Implements Strategy pattern for dynamic command module registration,
/// ArgsParser for tokenizing inputs, and IRenderer for visual display.
/// </summary>
public class CliFacade : ICliFacade
{
    private readonly List<ICommandStrategy> _strategies = [];
    private readonly Dictionary<string, Command> _commandsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly CommandExecutionContext _context;

    public IRenderer Renderer { get; set; }
    public IReadOnlyCollection<ICommandStrategy> Strategies => _strategies.AsReadOnly();
    public IReadOnlyCollection<Command> Commands => _commandsByName.Values.Distinct().ToList().AsReadOnly();

    public CliFacade(
        Game game,
        IRepositoryService repository,
        IGenshinApiClient apiClient,
        IRenderer? renderer = null,
        Func<string, string>? promptReader = null,
        IAssetCacheService? assetCache = null,
        IGameSaveManager? saveManager = null)
    {
        Renderer = renderer ?? new ConsoleRenderer();
        _context = new CommandExecutionContext(game, repository, apiClient, Renderer, promptReader, assetCache, saveManager);

        // Register default built-in strategies
        UseStrategy(new InformationCommandStrategy());
        UseStrategy(new TransformativeCommandStrategy());
        UseStrategy(new InfrastructureCommandStrategy());

        // Register global 'help' command
        RegisterBuiltInHelp();
    }

    public void UseStrategy(ICommandStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);

        if (!_strategies.Contains(strategy))
        {
            _strategies.Add(strategy);
        }

        foreach (var cmd in strategy.GetCommands())
        {
            _commandsByName[cmd.Name] = cmd;
            foreach (var alias in cmd.Aliases)
            {
                _commandsByName[alias] = cmd;
            }
        }
    }

    public CommandResult ExecCommand(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return CommandResult.Fail("Empty input command.");
        }

        var args = ArgsParser.Parse(rawInput);
        if (string.IsNullOrEmpty(args.CommandName))
        {
            return CommandResult.Fail("Command name missing.");
        }

        if (_commandsByName.TryGetValue(args.CommandName, out var command))
        {
            try
            {
                return command.Execute(args, _context);
            }
            catch (Exception ex)
            {
                var error = $"Error executing command '{args.CommandName}': {ex.Message}";
                Renderer.RenderStatus(error, false);
                return CommandResult.Fail(error);
            }
        }

        var unknownMsg = $"Unknown command '{args.CommandName}'. Type 'help' to inspect available commands.";
        Renderer.RenderStatus(unknownMsg, false);
        return CommandResult.Fail(unknownMsg);
    }

    public void Display(IRenderable renderable)
    {
        ArgumentNullException.ThrowIfNull(renderable);
        renderable.Render(Renderer);
    }

    public void Display(string message)
    {
        Renderer.RenderLine(message);
    }

    private void RegisterBuiltInHelp()
    {
        var helpCmd = new DelegateCommand(
            "help",
            "Lists all registered commands grouped by strategy categories.",
            "help [category]",
            "General",
            (args, ctx) =>
            {
                var filterCategory = args.GetArg(0);

                ctx.Renderer.RenderLine("=========================================================================");
                ctx.Renderer.RenderLine("           CLI COMMAND-LINE INTERFACE REFERENCE MANUAL                  ");
                ctx.Renderer.RenderLine("=========================================================================");

                foreach (var strat in _strategies)
                {
                    if (!string.IsNullOrEmpty(filterCategory) &&
                        !strat.Category.Equals(filterCategory, StringComparison.OrdinalIgnoreCase) &&
                        !strat.StrategyName.Contains(filterCategory, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var headers = new[] { "Command", "Usage", "Description" };
                    var rows = new List<IReadOnlyList<string>>();

                    foreach (var cmd in strat.GetCommands())
                    {
                        rows.Add([cmd.Name, cmd.Usage, cmd.Description]);
                    }

                    ctx.Renderer.RenderLine($"\n● Strategy: {strat.StrategyName} [{strat.Category}] — {strat.Description}");
                    ctx.Renderer.RenderTable(headers, rows);
                }

                return CommandResult.Ok("Help displayed.");
            },
            aliases: ["?"]
        );

        _commandsByName[helpCmd.Name] = helpCmd;
        foreach (var alias in helpCmd.Aliases)
        {
            _commandsByName[alias] = helpCmd;
        }
    }
}
