using Project.CLI.Commands;
using Project.CLI.Facade;
using Project.CLI.Rendering;
using Project.Core.Characters.Services;
using Project.Core.FileWorkspace.Canvas;
using Project.Core.FileWorkspace.FileSystem;
using Project.Core.FileWorkspace.Services;
using Project.Core.Text.Services;
using Project.Infra.Data.Repositories;
using Project.Infra.Display;
using Project.Infra.Remote;

namespace Project.CLI.Presenter;

/// <summary>
/// Console presenter coordinating input loops, mode selection, command dispatch, and interactive dialogs.
/// </summary>
public class ConsolePresenter : IPresenter
{
    private readonly IDisplayer _displayer;
    private readonly IRepositoryService _repository;
    private readonly Game _game;
    private readonly TextDocumentManager _textManager;
    private readonly IGenshinApiClient _apiClient;
    private readonly CommandRegistry _charRegistry;
    private readonly CommandRegistry _textRegistry;
    private readonly CliFacade _cliFacade;
    private string _activeMode = "chars"; // "chars" or "text"

    public CliFacade Facade => _cliFacade;

    public ConsolePresenter(
        IDisplayer displayer,
        IRepositoryService repository,
        Game game,
        TextDocumentManager textManager,
        IGenshinApiClient? apiClient = null)
    {
        _displayer = displayer;
        _repository = repository;
        _game = game;
        _textManager = textManager;
        _apiClient = apiClient ?? new GenshinApiClient(new HttpClient());
        _cliFacade = new CliFacade(game, repository, _apiClient, new ConsoleRenderer(displayer), ReadLine);

        _charRegistry = new CommandRegistry();
        _textRegistry = new CommandRegistry();

        CharactersCommands.Register(_charRegistry, ReadLine);
        TextCommands.Register(_textRegistry, ReadLine);
        ApiCommands.Register(_charRegistry, ReadLine);
        ApiCommands.Register(_textRegistry, ReadLine);

        // Register cross-cutting script command
        Action<ParsedCommand, CommandContext> runScriptAction = (cmd, ctx) =>
        {
            var file = cmd.Arguments.Count > 0 ? cmd.Arguments[0] : ReadLine("Script file path: ");
            RunScript(file);
        };
        _charRegistry.Register("run", "Executes scenario script from file.", "run <script_path>", runScriptAction);
        _textRegistry.Register("run", "Executes scenario script from file.", "run <script_path>", runScriptAction);

        // Register workspace state machine command
        Action<ParsedCommand, CommandContext> workspaceAction = (cmd, ctx) =>
        {
            LaunchWorkspace();
        };
        _charRegistry.Register("workspace", "Launches interactive Console File Workspace state machine.", "workspace", workspaceAction);
        _textRegistry.Register("workspace", "Launches interactive Console File Workspace state machine.", "workspace", workspaceAction);
    }

    public void Run(string[] args)
    {
        var launchCmd = ParsedCommand.Parse(string.Join(" ", args));

        if (launchCmd.HasOption("workspace") || args.Any(a => a.Equals("workspace", StringComparison.OrdinalIgnoreCase)))
        {
            LaunchWorkspace();
            return;
        }

        if (launchCmd.HasOption("chars"))
        {
            _activeMode = "chars";
        }
        else if (launchCmd.HasOption("text"))
        {
            _activeMode = "text";
        }

        if (launchCmd.HasOption("script") || launchCmd.HasOption("scenario"))
        {
            var scriptPath = launchCmd.GetOption("script") ?? launchCmd.GetOption("scenario");
            if (!string.IsNullOrEmpty(scriptPath))
            {
                RunScript(scriptPath);
                return;
            }
        }
        else
        {
            SelectModeDialog();
        }

        InteractiveLoop();
    }

    private void LaunchWorkspace()
    {
        var canvas = new VirtualConsoleCanvas(80, 24, _displayer);
        var fs = new RealFileSystemService();
        var engine = new WorkspaceEngine(fs, canvas);
        engine.RunInteractive(prompt => ReadLine(prompt));
    }

    private void SelectModeDialog()
    {
        _displayer.Display("=========================================================================");
        _displayer.Display("       MODULAR APPLICATION PLATFORM — OOP ARCHITECTURE (TDD MODULE)      ");
        _displayer.Display("   Clean Architecture • Domain Models • Remote REST API • Structured Text");
        _displayer.Display("=========================================================================");
        _displayer.Display("Select operating subsystem mode:");
        _displayer.Display("  [1] Characters RPG Combat & Inventory Engine");
        _displayer.Display("  [2] Structured Hierarchical Document Editor");
        _displayer.Display("  [3] Console File Workspace (Explorer / Viewer / Editor State Machine)");
        _displayer.Display("  [4] Run Automated Verification Scenarios");
        _displayer.Display("  [5] Quick Demo: Import Character & Weapon from Remote REST API");
        _displayer.Display("  [6] Exit\n");

        var choice = ReadLine("Select option [1-6] (default: 1): ");
        switch (choice.Trim())
        {
            case "2":
                _activeMode = "text";
                break;
            case "3":
                LaunchWorkspace();
                SelectModeDialog();
                break;
            case "4":
                RunDefaultScenarios();
                SelectModeDialog();
                break;
            case "5":
                RunRemoteApiDemo();
                SelectModeDialog();
                break;
            case "6":
                Environment.Exit(0);
                break;
            default:
                _activeMode = "chars";
                break;
        }
    }

    private void RunRemoteApiDemo()
    {
        _displayer.Display("\n>>> Running Remote REST API Import Demo <<<");
        var context = new CommandContext(_game, _textManager, _repository, _displayer, _apiClient);
        _charRegistry.Execute(ParsedCommand.Parse("api import raiden engulfing-lightning"), context);
    }

    private void InteractiveLoop()
    {
        var context = new CommandContext(_game, _textManager, _repository, _displayer, _apiClient);

        _displayer.Display($"\n● Active Mode: {_activeMode.ToUpperInvariant()}");
        _displayer.Display("Type 'help' for commands, 'api' to query remote database, 'switch' to toggle subsystems, 'exit' to quit.\n");

        while (!context.ShouldExit)
        {
            var promptPrefix = _activeMode == "chars"
                ? "[CHARS]> "
                : $"[TEXT:{_textManager.CurrentNode.Name}]> ";
            var input = ReadLine(promptPrefix);

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            var cmd = ParsedCommand.Parse(input);
            var activeRegistry = _activeMode == "chars" ? _charRegistry : _textRegistry;

            if (activeRegistry.HasCommand(cmd.Name))
            {
                activeRegistry.Execute(cmd, context);
            }
            else if (_activeMode == "chars")
            {
                var result = _cliFacade.ExecCommand(input);
                if (result.Success && result.Message == "Exit requested.")
                {
                    context.ShouldExit = true;
                }
            }
            else
            {
                activeRegistry.Execute(cmd, context);
            }

            if (!string.IsNullOrEmpty(context.SwitchToMode))
            {
                _activeMode = context.SwitchToMode;
                context.SwitchToMode = null;
                _displayer.Display($"Switched active mode to: {_activeMode.ToUpperInvariant()}");
            }
        }
    }

    public string ReadLine(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine() ?? string.Empty;
    }

    public void RunScript(string scriptFilePath)
    {
        if (!File.Exists(scriptFilePath))
        {
            _displayer.Display($"Script file not found: {scriptFilePath}");
            return;
        }

        _displayer.Display($"\n>>> Executing script: {scriptFilePath} <<<");
        var lines = File.ReadAllLines(scriptFilePath);
        var context = new CommandContext(_game, _textManager, _repository, _displayer, _apiClient);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                continue;
            }

            _displayer.Display($"\n> {line}");

            if (line.Equals("switch", StringComparison.OrdinalIgnoreCase))
            {
                _activeMode = _activeMode == "chars" ? "text" : "chars";
                _displayer.Display($"Switched mode to: {_activeMode.ToUpperInvariant()}");
                continue;
            }

            if (line.StartsWith("mode ", StringComparison.OrdinalIgnoreCase))
            {
                var modeArg = line.Substring(5).Trim().ToLowerInvariant();
                _activeMode = modeArg == "text" ? "text" : "chars";
                _displayer.Display($"Switched mode to: {_activeMode.ToUpperInvariant()}");
                continue;
            }

            var cmd = ParsedCommand.Parse(line);
            var activeRegistry = _activeMode == "chars" ? _charRegistry : _textRegistry;
            activeRegistry.Execute(cmd, context);
        }

        _displayer.Display($"\n>>> Completed script execution: {scriptFilePath} <<<\n");
    }

    private void RunDefaultScenarios()
    {
        var gameScenario = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scenarios", "game_scenario.txt");
        if (!File.Exists(gameScenario))
        {
            gameScenario = Path.Combine("Scenarios", "game_scenario.txt");
        }

        var textScenario = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scenarios", "text_scenario.txt");
        if (!File.Exists(textScenario))
        {
            textScenario = Path.Combine("Scenarios", "text_scenario.txt");
        }

        if (File.Exists(gameScenario))
        {
            _activeMode = "chars";
            RunScript(gameScenario);
        }

        if (File.Exists(textScenario))
        {
            _activeMode = "text";
            RunScript(textScenario);
        }
    }
}
