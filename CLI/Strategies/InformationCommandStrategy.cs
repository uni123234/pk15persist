using Project.CLI.Core;

namespace Project.CLI.Strategies;

/// <summary>
/// Strategy 1: Information / Query commands.
/// Responsible for inspecting system entities, rendering catalog tables, and displaying character cards.
/// </summary>
public class InformationCommandStrategy : ICommandStrategy
{
    public string StrategyName => "Information Strategy";
    public string Category => "Query";
    public string Description => "Read-only queries, catalog overviews, and detailed entity cards.";

    public IReadOnlyList<Command> GetCommands() =>
    [
        new DelegateCommand(
            "chars",
            "Displays the formatted catalog of all registered characters with visual avatar links.",
            "chars [--id <id/name>]",
            Category,
            ExecuteListCharacters,
            aliases: ["list-chars", "characters"]
        ),
        new DelegateCommand(
            "char",
            "Displays a detailed visual card of a specific character including graphic avatar URL.",
            "char <id/name>",
            Category,
            ExecuteShowCharacter,
            aliases: ["show-char"]
        ),
        new DelegateCommand(
            "items",
            "Displays the catalog table of all registered items and equipment.",
            "items [--id <id/name>]",
            Category,
            ExecuteListItems,
            aliases: ["list-items"]
        ),
        new DelegateCommand(
            "abilities",
            "Displays the grimoire catalog of all known active skills and spells.",
            "abilities [--id <id/name>]",
            Category,
            ExecuteListAbilities,
            aliases: ["list-abilities"]
        )
    ];

    private static CommandResult ExecuteListCharacters(Args args, CommandExecutionContext ctx)
    {
        var targetId = args.GetOption("id") ?? args.GetArg(0);
        if (!string.IsNullOrEmpty(targetId))
        {
            return ExecuteShowCharacter(args, ctx);
        }

        var characters = ctx.Game.Characters;
        if (characters.Count == 0)
        {
            ctx.Renderer.RenderLine("  (No characters registered in the catalog)");
            return CommandResult.Ok("Character catalog is empty.");
        }

        var headers = new[] { "ID", "Name", "Health", "Power", "Res", "Items", "Portrait Cache Status" };
        var rows = new List<IReadOnlyList<string>>();

        foreach (var ch in characters)
        {
            var status = ch.IsAlive ? $"{ch.Health}/{ch.MaxHealth}" : "DEFEATED";
            var isCached = ctx.AssetCache.IsCached(ch.Name);
            var cacheInfo = isCached ? $"[CACHED] {Path.GetFileName(ctx.AssetCache.GetLocalFilePath(ch.Name))}" : "[REMOTE]";
            rows.Add([
                ch.Id.ToString(),
                ch.Name,
                status,
                ch.Power.ToString(),
                $"{ch.Resistance:P0}",
                ch.ActiveItems.Count.ToString(),
                cacheInfo
            ]);
        }

        ctx.Renderer.RenderTable(headers, rows);
        ctx.Renderer.RenderLine($"Total characters: {characters.Count}");
        return CommandResult.Ok($"Listed {characters.Count} characters.", characters);
    }

    private static CommandResult ExecuteShowCharacter(Args args, CommandExecutionContext ctx)
    {
        var nameOrId = args.GetArg(0) ?? args.GetOption("id");
        if (string.IsNullOrEmpty(nameOrId))
        {
            nameOrId = ctx.PromptReader("Enter Character ID or Name: ");
        }

        var ch = ctx.Game.FindCharacter(nameOrId);
        if (ch == null)
        {
            ctx.Renderer.RenderStatus($"Character '{nameOrId}' not found in catalog.", false);
            return CommandResult.Fail($"Character '{nameOrId}' not found.");
        }

        var avatarUrl = $"https://genshin.jmp.blue/characters/{ch.Name.ToLowerInvariant().Replace(' ', '-')}/icon";
        var isCached = ctx.AssetCache.IsCached(ch.Name);
        var localPortraitPath = ctx.AssetCache.GetLocalFilePath(ch.Name);
        var portraitDisplay = isCached ? $"[LOCAL BUFFER] {localPortraitPath}" : $"[REMOTE] {avatarUrl}";
        var itemsList = ch.ActiveItems.Count > 0 ? string.Join(", ", ch.ActiveItems.Select(i => i.Name)) : "(None)";
        var abilitiesList = ch.AvailableAbilities.Count > 0 ? string.Join(", ", ch.AvailableAbilities.Select(a => a.Name)) : "(None)";

        var lines = new[]
        {
            $"ID: #{ch.Id}   |   Name: {ch.Name}   |   Status: {(ch.IsAlive ? "ALIVE" : "DEFEATED")}",
            $"Health: {ch.Health}/{ch.MaxHealth} (Base: {ch.BaseHealth})",
            $"Power:  {ch.Power} (Base: {ch.BasePower})",
            $"Armor:  {ch.Armor} (Base: {ch.BaseArmor}) | Speed: {ch.Speed} | Res: {ch.Resistance:P0}",
            $"Portrait: {portraitDisplay}",
            "─────────────────────────────────────────────────────",
            $"Equipped Items:    {itemsList}",
            $"Learned Abilities: {abilitiesList}"
        };

        ctx.Renderer.RenderBox($"CHARACTER PROFILE: {ch.Name.ToUpperInvariant()}", lines);
        return CommandResult.Ok($"Displayed profile for {ch.Name}.", ch);
    }

    private static CommandResult ExecuteListItems(Args args, CommandExecutionContext ctx)
    {
        var items = ctx.Game.Items;
        if (items.Count == 0)
        {
            ctx.Renderer.RenderLine("  (No items registered in catalog)");
            return CommandResult.Ok("Item catalog is empty.");
        }

        var headers = new[] { "ID", "Name", "HP Bonus", "Power Bonus", "Equipped By" };
        var rows = new List<IReadOnlyList<string>>();

        foreach (var it in items)
        {
            var owners = ctx.Game.Characters
                .Where(c => c.ActiveItems.Any(i => i.Id == it.Id))
                .Select(c => c.Name);
            var equippedBy = owners.Any() ? string.Join(", ", owners) : "(In storage)";

            rows.Add([
                it.Id.ToString(),
                it.Name,
                $"+{it.HpMod}",
                $"+{it.PowerMod}",
                equippedBy
            ]);
        }

        ctx.Renderer.RenderTable(headers, rows);
        return CommandResult.Ok($"Listed {items.Count} items.");
    }

    private static CommandResult ExecuteListAbilities(Args args, CommandExecutionContext ctx)
    {
        var abilities = ctx.Game.Abilities;
        if (abilities.Count == 0)
        {
            ctx.Renderer.RenderLine("  (No abilities registered in catalog)");
            return CommandResult.Ok("Ability catalog is empty.");
        }

        var headers = new[] { "ID", "Name", "Damage", "Healing", "Description" };
        var rows = new List<IReadOnlyList<string>>();

        foreach (var ab in abilities)
        {
            rows.Add([
                ab.Id.ToString(),
                ab.Name,
                ab.Damage > 0 ? ab.Damage.ToString() : "-",
                ab.Heal > 0 ? ab.Heal.ToString() : "-",
                ab.Description
            ]);
        }

        ctx.Renderer.RenderTable(headers, rows);
        return CommandResult.Ok($"Listed {abilities.Count} abilities.");
    }
}
