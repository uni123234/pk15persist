using Project.Infra.Remote;
using Project.Infra.Remote.Models;

namespace Project.CLI.Commands;

/// <summary>
/// Registers CLI commands for communicating with the remote REST API,
/// querying database entries, and importing characters as structured text.
/// Fulfills Requirement 4 of Practical Work 7.
/// </summary>
public static class ApiCommands
{
    public static void Register(CommandRegistry registry, Func<string, string> promptReader)
    {
        // 1. api <chars|weapons|import|view>
        registry.Register(
            "api",
            "Queries remote REST API, lists catalog, and imports character with structured text representation.",
            "api <chars|weapons|import|view> [name] [weapon_name]",
            (cmd, ctx) =>
            {
                var subCommand = cmd.Arguments.Count > 0 ? cmd.Arguments[0].ToLowerInvariant() : "";
                if (string.IsNullOrEmpty(subCommand))
                {
                    ctx.Displayer.Display("Remote API Commands:\n  1) chars   - List remote characters\n  2) weapons - List remote weapons\n  3) import  - Import character & weapon\n  4) view    - View structured text profile");
                    var choice = promptReader("Select option (1-4): ");
                    subCommand = choice switch
                    {
                        "1" => "chars",
                        "2" => "weapons",
                        "3" => "import",
                        "4" => "view",
                        _ => choice.ToLowerInvariant()
                    };
                }

                switch (subCommand)
                {
                    case "chars":
                    case "characters":
                        ListCharacters(ctx);
                        break;

                    case "weapons":
                        ListWeapons(ctx);
                        break;

                    case "import":
                    {
                        var charName = cmd.Arguments.Count > 1 ? cmd.Arguments[1] : promptReader("Character Name (e.g. raiden, zhongli, hu-tao): ");
                        var weaponName = cmd.Arguments.Count > 2 ? cmd.Arguments[2] : cmd.GetOption("weapon");
                        ImportCharacter(ctx, charName, weaponName);
                        break;
                    }

                    case "view":
                    {
                        var charName = cmd.Arguments.Count > 1 ? cmd.Arguments[1] : promptReader("Character Name: ");
                        ViewCharacterProfile(ctx, charName);
                        break;
                    }

                    default:
                        ctx.Displayer.Display($"Unknown API subcommand '{subCommand}'. Available: chars, weapons, import, view.");
                        break;
                }
            });

        // Alias: fetch
        registry.Register(
            "fetch",
            "Alias for 'api import <char_name> [weapon_name]'.",
            "fetch <char_name> [weapon_name]",
            (cmd, ctx) =>
            {
                if (cmd.Arguments.Count == 0)
                {
                    ctx.Displayer.Display("Usage: fetch <char_name> [weapon_name]");
                    return;
                }
                var charName = cmd.Arguments[0];
                var weaponName = cmd.Arguments.Count > 1 ? cmd.Arguments[1] : null;
                ImportCharacter(ctx, charName, weaponName);
            });
    }

    private static void ListCharacters(CommandContext ctx)
    {
        ctx.Displayer.Display("Querying remote REST API for available characters...");
        try
        {
            var task = ctx.ApiClient.GetCharacterNamesAsync();
            task.Wait();
            var names = task.Result;

            ctx.Displayer.Display($"--- Remote Characters Catalog ({names.Count} entries) ---");
            for (var i = 0; i < names.Count; i += 4)
            {
                var row = names.Skip(i).Take(4);
                ctx.Displayer.Display(string.Join("\t\t", row));
            }
        }
        catch (Exception ex)
        {
            ctx.Displayer.Display($"[Error] Failed to fetch characters: {ex.Message}");
        }
    }

    private static void ListWeapons(CommandContext ctx)
    {
        ctx.Displayer.Display("Querying remote REST API for available weapons...");
        try
        {
            var task = ctx.ApiClient.GetWeaponNamesAsync();
            task.Wait();
            var names = task.Result;

            ctx.Displayer.Display($"--- Remote Weapons Catalog ({names.Count} entries) ---");
            for (var i = 0; i < names.Count; i += 4)
            {
                var row = names.Skip(i).Take(4);
                ctx.Displayer.Display(string.Join("\t\t", row));
            }
        }
        catch (Exception ex)
        {
            ctx.Displayer.Display($"[Error] Failed to fetch weapons: {ex.Message}");
        }
    }

    private static void ImportCharacter(CommandContext ctx, string charName, string? weaponName)
    {
        if (string.IsNullOrWhiteSpace(charName))
        {
            ctx.Displayer.Display("[Error] Character name cannot be empty.");
            return;
        }

        ctx.Displayer.Display($"[API] Fetching character data for '{charName}'...");
        var charTask = ctx.ApiClient.GetCharacterAsync(charName);
        charTask.Wait();
        var remoteChar = charTask.Result;

        if (remoteChar == null)
        {
            ctx.Displayer.Display($"[Error] Character '{charName}' was not found in remote database.");
            return;
        }

        RemoteWeaponData? remoteWeapon = null;
        if (!string.IsNullOrWhiteSpace(weaponName))
        {
            ctx.Displayer.Display($"[API] Fetching weapon data for '{weaponName}'...");
            var weaponTask = ctx.ApiClient.GetWeaponAsync(weaponName);
            weaponTask.Wait();
            remoteWeapon = weaponTask.Result;
        }

        // Map remote data to domain models
        var nextCharId = ctx.Game.Characters.Count + 1;
        var domainChar = RemoteDataMapper.ToCharacter(remoteChar, nextCharId);

        if (remoteWeapon != null)
        {
            var nextItemId = ctx.Game.Items.Count + 1;
            var domainWeapon = RemoteDataMapper.ToItem(remoteWeapon, nextItemId);
            domainChar.AddItem(domainWeapon);
        }

        // Add to active Game domain session
        var addedChar = ctx.Game.CreateCharacter(
            domainChar.Name,
            domainChar.Health,
            domainChar.Power,
            domainChar.Resistance,
            domainChar.Abilities,
            domainChar.EquippedItems
        );

        ctx.Displayer.Display($"Successfully imported virtual character: {addedChar.Name} (ID #{addedChar.Id})");

        // Build Structured Text representation and render to console
        ctx.Displayer.Display("\n=== GENERATED STRUCTURED TEXT PROFILE (Core.Text) ===");
        var docManager = CharacterTextProfileBuilder.BuildProfile(domainChar, remoteChar, ctx.Displayer, remoteWeapon);
        docManager.Print(whole: true);
        ctx.Displayer.Display("====================================================\n");
    }

    private static void ViewCharacterProfile(CommandContext ctx, string charName)
    {
        var match = ctx.Game.Characters.FirstOrDefault(c => c.Name.Equals(charName, StringComparison.OrdinalIgnoreCase));
        if (match == null)
        {
            ctx.Displayer.Display($"Character '{charName}' is not loaded in current game session.");
            return;
        }

        var charTask = ctx.ApiClient.GetCharacterAsync(charName);
        charTask.Wait();
        var remoteChar = charTask.Result ?? new RemoteCharacterData
        {
            Name = match.Name,
            Rarity = 5,
            Description = "Locally registered character."
        };

        var docManager = CharacterTextProfileBuilder.BuildProfile(match, remoteChar, ctx.Displayer);
        docManager.Print(whole: true);
    }
}
