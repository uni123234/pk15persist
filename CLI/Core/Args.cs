using System.Text.RegularExpressions;

namespace Project.CLI.Core;

/// <summary>
/// Strongly-typed object-oriented representation of parsed terminal arguments, options, and flags.
/// </summary>
public class Args
{
    public string RawInput { get; }
    public string CommandName { get; }
    public IReadOnlyList<string> PositionalArgs { get; }
    public IReadOnlyDictionary<string, string> Options { get; }
    public IReadOnlySet<string> Flags { get; }

    public Args(
        string rawInput,
        string commandName,
        IEnumerable<string> positionalArgs,
        IDictionary<string, string> options,
        ISet<string> flags)
    {
        RawInput = rawInput;
        CommandName = commandName.ToLowerInvariant();
        PositionalArgs = positionalArgs.ToList().AsReadOnly();
        Options = new Dictionary<string, string>(options, StringComparer.OrdinalIgnoreCase);
        Flags = new HashSet<string>(flags, StringComparer.OrdinalIgnoreCase);
    }

    public string? GetArg(int index, string? defaultValue = null)
    {
        return index >= 0 && index < PositionalArgs.Count ? PositionalArgs[index] : defaultValue;
    }

    public string? GetOption(string key, string? defaultValue = null)
    {
        return Options.TryGetValue(key, out var val) ? val : defaultValue;
    }

    public bool HasOption(string key) => Options.ContainsKey(key);

    public bool HasFlag(string flag) => Flags.Contains(flag);

    public T GetOption<T>(string key, T defaultValue) where T : IParsable<T>
    {
        if (Options.TryGetValue(key, out var raw) && T.TryParse(raw, null, out var parsed))
        {
            return parsed;
        }
        return defaultValue;
    }
}

/// <summary>
/// Parser converting raw command lines into structured Args instances.
/// Correctly handles quotes, positional arguments, --key value options, and --flag flags.
/// </summary>
public static class ArgsParser
{
    private static readonly Regex TokenRegex = new(@"[\""].+?[\""]|[^ \t\""]+", RegexOptions.Compiled);

    public static Args Parse(string rawInput)
    {
        ArgumentNullException.ThrowIfNull(rawInput);
        var trimmed = rawInput.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return new Args(rawInput, "", [], new Dictionary<string, string>(), new HashSet<string>());
        }

        var matches = TokenRegex.Matches(trimmed);
        var tokens = matches.Select(m => m.Value.Trim('"')).ToList();

        if (tokens.Count == 0)
        {
            return new Args(rawInput, "", [], new Dictionary<string, string>(), new HashSet<string>());
        }

        var commandName = tokens[0];
        var positional = new List<string>();
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var i = 1;
        while (i < tokens.Count)
        {
            var token = tokens[i];
            if (token.StartsWith("--") || token.StartsWith('-'))
            {
                var optName = token.TrimStart('-');
                // Check if next token exists and is a value (not another flag)
                if (i + 1 < tokens.Count && !tokens[i + 1].StartsWith('-'))
                {
                    options[optName] = tokens[i + 1];
                    flags.Add(optName); // Also available as flag
                    i += 2;
                }
                else
                {
                    flags.Add(optName);
                    i++;
                }
            }
            else
            {
                positional.Add(token);
                i++;
            }
        }

        return new Args(rawInput, commandName, positional, options, flags);
    }
}
