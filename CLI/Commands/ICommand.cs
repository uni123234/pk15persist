using Project.Core.Characters.Services;
using Project.Core.Text.Services;
using Project.Infra.Data.Repositories;
using Project.Infra.Display;
using Project.Infra.Remote;

namespace Project.CLI.Commands;

/// <summary>
/// Encapsulates a parsed command line string with arguments, options and flags.
/// </summary>
public class ParsedCommand
{
    public string Name { get; set; } = string.Empty;
    public List<string> Arguments { get; } = [];
    public Dictionary<string, string> Options { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool HasOption(string name) => Options.ContainsKey(name);

    public string? GetOption(string name, string? defaultValue = null)
    {
        return Options.TryGetValue(name, out var val) ? val : defaultValue;
    }

    public static ParsedCommand Parse(string input)
    {
        var result = new ParsedCommand();
        if (string.IsNullOrWhiteSpace(input))
        {
            return result;
        }

        var tokens = Tokenize(input);
        if (tokens.Count == 0)
        {
            return result;
        }

        var startIndex = 0;
        if (!tokens[0].StartsWith("-"))
        {
            result.Name = tokens[0].ToLowerInvariant();
            startIndex = 1;
        }

        for (var i = startIndex; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.StartsWith("--"))
            {
                var optKey = token.Substring(2);
                if (i + 1 < tokens.Count && !tokens[i + 1].StartsWith("-"))
                {
                    result.Options[optKey] = tokens[i + 1];
                    i++;
                }
                else
                {
                    result.Options[optKey] = "true";
                }
            }
            else if (token.StartsWith("-") && token.Length > 1)
            {
                var optKey = token.Substring(1);
                if (i + 1 < tokens.Count && !tokens[i + 1].StartsWith("-"))
                {
                    result.Options[optKey] = tokens[i + 1];
                    i++;
                }
                else
                {
                    result.Options[optKey] = "true";
                }
            }
            else
            {
                result.Arguments.Add(token);
            }
        }

        return result;
    }

    private static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var inQuotes = false;
        var quoteChar = '\0';
        var current = new System.Text.StringBuilder();

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];

            if ((c == '"' || c == '\'') && !inQuotes)
            {
                inQuotes = true;
                quoteChar = c;
            }
            else if (inQuotes && c == quoteChar)
            {
                inQuotes = false;
                quoteChar = '\0';
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }
}

/// <summary>
/// Execution context injected into commands providing access to services and presentational facilities.
/// </summary>
public class CommandContext(
    Game game,
    TextDocumentManager textManager,
    IRepositoryService repository,
    IDisplayer displayer,
    IGenshinApiClient? apiClient = null)
{
    public Game Game { get; } = game;
    public TextDocumentManager TextManager { get; } = textManager;
    public IRepositoryService Repository { get; } = repository;
    public IDisplayer Displayer { get; } = displayer;
    public IGenshinApiClient ApiClient { get; } = apiClient ?? new GenshinApiClient(new HttpClient());
    public bool ShouldExit { get; set; }
    public string? SwitchToMode { get; set; }
}

/// <summary>
/// Interface for dynamic CLI commands.
/// </summary>
public interface ICommand
{
    string Name { get; }
    string Description { get; }
    string Usage { get; }
    void Execute(ParsedCommand command, CommandContext context);
}

/// <summary>
/// Generic action-based command delegate wrapper.
/// </summary>
public class DelegateCommand(string name, string description, string usage, Action<ParsedCommand, CommandContext> handler) : ICommand
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public string Usage { get; } = usage;

    public void Execute(ParsedCommand command, CommandContext context)
    {
        handler(command, context);
    }
}
