using Project.CLI.Core;
using Project.CLI.Rendering;
using Project.Infra.Remote;

namespace Project.CLI.Strategies;

/// <summary>
/// Strategy 3: Infrastructure / System commands.
/// Handles persistence (saving/loading/continuation), asset caching/buffering, remote API integrations, and session control.
/// </summary>
public class InfrastructureCommandStrategy : ICommandStrategy
{
    public string StrategyName => "Infrastructure System Strategy";
    public string Category => "Infrastructure";
    public string Description => "Persistence storage, Save/Load continuation, Asset buffering, Remote API, and diagnostics.";

    public IReadOnlyList<Command> GetCommands() =>
    [
        new DelegateCommand(
            "save",
            "Persists the current game state atomically into a named save slot.",
            "save [save_name]",
            Category,
            ExecuteSave
        ),
        new DelegateCommand(
            "load",
            "Restores a previously saved state from a specified slot.",
            "load [save_name]",
            Category,
            ExecuteLoad
        ),
        new DelegateCommand(
            "continue",
            "Resumes the game from the most recent save slot.",
            "continue",
            Category,
            ExecuteContinue,
            aliases: ["resume"]
        ),
        new DelegateCommand(
            "saves",
            "Lists all available game save slots with timestamps and character counts.",
            "saves",
            Category,
            ExecuteListSaves
        ),
        new DelegateCommand(
            "cache",
            "Manages local asset portrait caching and buffering (status, fetch <name>, prefetch, clear).",
            "cache <status|fetch|prefetch|clear> [args...]",
            Category,
            ExecuteCache
        ),
        new DelegateCommand(
            "api-import",
            "Imports character profile and weapon from remote REST API with visual progress tracking.",
            "api-import <character_id> [weapon_id]",
            Category,
            ExecuteApiImport,
            aliases: ["import"]
        ),
        new DelegateCommand(
            "api-query",
            "Fetches and displays live character data from remote REST API.",
            "api-query <character_id>",
            Category,
            ExecuteApiQuery
        ),
        new DelegateCommand(
            "status",
            "Displays current session diagnostics, registered counts, cache metrics, and repository status.",
            "status",
            Category,
            ExecuteStatus
        ),
        new DelegateCommand(
            "exit",
            "Exits the current CLI interactive session.",
            "exit",
            Category,
            (args, ctx) =>
            {
                ctx.ShouldExit = true;
                ctx.Renderer.RenderStatus("Exiting CLI session.", true);
                return CommandResult.Ok("Exit requested.");
            },
            aliases: ["quit", "q"]
        )
    ];

    private static CommandResult ExecuteSave(Args args, CommandExecutionContext ctx)
    {
        var name = args.GetArg(0);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"AutoSave_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
        }

        var meta = ctx.SaveManager.Save(name, ctx.Game);
        var save = ctx.Game.ExportSave(name);
        ctx.Repository.SaveGameState(name, save);

        ctx.Renderer.RenderStatus($"Game state successfully saved to slot '{meta.SlotName}' ({meta.CharacterCount} characters, {meta.FileSizeBytes} bytes).", true);
        return CommandResult.Ok($"Saved state '{meta.SlotName}'.", meta);
    }

    private static CommandResult ExecuteLoad(Args args, CommandExecutionContext ctx)
    {
        var name = args.GetArg(0);
        if (string.IsNullOrWhiteSpace(name))
        {
            var saves = ctx.SaveManager.ListSaves();
            if (saves.Count == 0)
            {
                ctx.Renderer.RenderStatus("No saved states found.", false);
                return CommandResult.Fail("No saves available.");
            }

            ctx.Renderer.RenderLine("Available save slots: " + string.Join(", ", saves.Select(s => s.SlotName)));
            name = ctx.PromptReader("Enter save slot to restore: ");
        }

        try
        {
            var loaded = ctx.SaveManager.Load(name);
            ctx.Game.ImportSave(loaded);
            ctx.Renderer.RenderStatus($"Restored game state from slot '{loaded.SaveName}'.", true);
            return CommandResult.Ok($"Loaded state '{loaded.SaveName}'.", loaded);
        }
        catch (Exception ex)
        {
            ctx.Renderer.RenderStatus($"Failed to restore state: {ex.Message}", false);
            return CommandResult.Fail(ex.Message);
        }
    }

    private static CommandResult ExecuteContinue(Args args, CommandExecutionContext ctx)
    {
        var latest = ctx.SaveManager.GetLatestSave();
        if (latest == null)
        {
            ctx.Renderer.RenderStatus("No previous save found to continue from.", false);
            return CommandResult.Fail("No save available.");
        }

        try
        {
            var restored = ctx.SaveManager.ContinueFromLatest(ctx.Game);
            var charList = string.Join(", ", ctx.Game.Characters.Select(c => c.Name));
            ctx.Renderer.RenderBox("CONTINUED FROM LAST SAVE",
            [
                $"Slot Name:        {latest.SlotName}",
                $"Saved At:         {latest.Timestamp:yyyy-MM-dd HH:mm:ss} UTC",
                $"Characters ({latest.CharacterCount}): {charList}",
                $"File Size:        {latest.FileSizeBytes} bytes"
            ]);
            ctx.Renderer.RenderStatus($"Successfully continued session from '{latest.SlotName}'.", true);
            return CommandResult.Ok($"Continued from {latest.SlotName}.", restored);
        }
        catch (Exception ex)
        {
            ctx.Renderer.RenderStatus($"Failed to continue from save: {ex.Message}", false);
            return CommandResult.Fail(ex.Message);
        }
    }

    private static CommandResult ExecuteListSaves(Args args, CommandExecutionContext ctx)
    {
        var saves = ctx.SaveManager.ListSaves();
        if (saves.Count == 0)
        {
            ctx.Renderer.RenderStatus("No game save slots found in storage.", false);
            return CommandResult.Ok("No saves.", saves);
        }

        var headers = new[] { "Slot", "Timestamp (UTC)", "Chars", "Items", "Abilities", "Size (B)", "Characters" };
        var rows = saves.Select(s => new[]
        {
            s.SlotName,
            s.Timestamp.ToString("yyyy-MM-dd HH:mm"),
            s.CharacterCount.ToString(),
            s.ItemCount.ToString(),
            s.AbilityCount.ToString(),
            s.FileSizeBytes.ToString(),
            string.Join(", ", s.CharacterNames)
        }).ToList();

        var table = CLIGraphic.FormatTable(headers, rows);
        ctx.Renderer.RenderLine(table);
        return CommandResult.Ok("Listed saves.", saves);
    }

    private static CommandResult ExecuteCache(Args args, CommandExecutionContext ctx)
    {
        var sub = args.GetArg(0)?.ToLowerInvariant() ?? "status";

        switch (sub)
        {
            case "status":
            {
                var stats = ctx.AssetCache.GetStatistics();
                var lines = new[]
                {
                    $"Cache Directory:  {stats.CacheDirectory}",
                    $"Cached Portraits: {stats.TotalCachedFiles}",
                    $"Total Size:       {stats.TotalSizeBytes:N0} bytes ({stats.TotalSizeBytes / 1024.0:F2} KB)",
                    $"Cache Hits:       {stats.CacheHits}",
                    $"Cache Misses:     {stats.CacheMisses}",
                    $"Hit Ratio:        {stats.HitRatio * 100:F1}%"
                };
                ctx.Renderer.RenderBox("LOCAL ASSET CACHE METRICS", lines);
                return CommandResult.Ok("Cache status displayed.", stats);
            }

            case "fetch":
            {
                var charName = args.GetArg(1);
                if (string.IsNullOrWhiteSpace(charName))
                {
                    charName = ctx.PromptReader("Character name to cache portrait: ");
                    if (string.IsNullOrWhiteSpace(charName))
                        return CommandResult.Fail("Character name cannot be empty.");
                }

                var url = args.GetArg(2);
                var localPath = ctx.AssetCache.GetOrDownloadPortraitAsync(charName, url).GetAwaiter().GetResult();
                ctx.Renderer.RenderStatus($"Buffered portrait for '{charName}' -> {localPath}", true);
                return CommandResult.Ok($"Cached portrait {charName}.", localPath);
            }

            case "prefetch":
            {
                var names = ctx.Game.Characters.Select(c => c.Name).ToList();
                if (names.Count == 0)
                {
                    names.AddRange(["jean", "diluc", "raiden", "zhongli", "venti"]);
                }

                ctx.Renderer.RenderLine($"Prefetching portraits for {names.Count} characters...");
                var paths = ctx.AssetCache.PrefetchPortraitsAsync(names).GetAwaiter().GetResult();
                ctx.Renderer.RenderStatus($"Successfully buffered {paths.Count} character portraits.", true);
                return CommandResult.Ok("Prefetched portraits.", paths);
            }

            case "clear":
            {
                ctx.AssetCache.ClearCache();
                ctx.Renderer.RenderStatus("Local portrait cache directory successfully cleared.", true);
                return CommandResult.Ok("Cache cleared.");
            }

            default:
                return CommandResult.Fail($"Unknown cache subcommand '{sub}'. Supported: status, fetch, prefetch, clear.");
        }
    }

    private static CommandResult ExecuteApiImport(Args args, CommandExecutionContext ctx)
    {
        var charId = args.GetArg(0);
        if (string.IsNullOrWhiteSpace(charId))
        {
            charId = ctx.PromptReader("Remote character ID to import (e.g., 'raiden', 'zhongli'): ");
            if (string.IsNullOrWhiteSpace(charId))
                return CommandResult.Fail("Character ID cannot be empty.");
        }

        ctx.Renderer.RenderProgressBar(1, 4, $"Connecting to remote REST API for '{charId}'");
        var remoteChar = ctx.ApiClient.GetCharacterAsync(charId).GetAwaiter().GetResult();
        if (remoteChar == null)
        {
            ctx.Renderer.RenderStatus($"Character '{charId}' not found on remote server.", false);
            return CommandResult.Fail("Character not found.");
        }

        ctx.Renderer.RenderProgressBar(2, 4, $"Mapping remote entity '{remoteChar.Name}'");
        var localChar = RemoteDataMapper.MapCharacter(remoteChar);

        // Also buffer the portrait locally!
        ctx.Renderer.RenderProgressBar(3, 4, $"Buffering local portrait for '{localChar.Name}'");
        var portraitUrl = $"https://genshin.jmp.blue/characters/{charId}/icon-big";
        ctx.AssetCache.GetOrDownloadPortraitAsync(localChar.Name, portraitUrl).GetAwaiter().GetResult();

        ctx.Renderer.RenderProgressBar(4, 4, "Registering domain entities");
        ctx.Game.CreateCharacter(localChar.Name, localChar.BaseHealth, localChar.BasePower, localChar.BaseResistance);

        ctx.Renderer.RenderStatus($"Imported remote entity '{localChar.Name}' into local catalog and buffered portrait.", true);
        return CommandResult.Ok($"Imported {localChar.Name}.", localChar);
    }

    private static CommandResult ExecuteApiQuery(Args args, CommandExecutionContext ctx)
    {
        var charId = args.GetArg(0);
        if (string.IsNullOrWhiteSpace(charId))
        {
            charId = ctx.PromptReader("Remote character ID to query: ");
            if (string.IsNullOrWhiteSpace(charId))
                return CommandResult.Fail("Character ID cannot be empty.");
        }

        var data = ctx.ApiClient.GetCharacterAsync(charId).GetAwaiter().GetResult();
        if (data == null)
        {
            ctx.Renderer.RenderStatus($"Remote character '{charId}' not found.", false);
            return CommandResult.Fail("Character not found.");
        }

        var isCached = ctx.AssetCache.IsCached(data.Name);
        var localPortrait = ctx.AssetCache.GetLocalFilePath(data.Name);

        var lines = new[]
        {
            $"Remote Name:    {data.Name}",
            $"Vision/Element: {data.Vision}",
            $"Weapon Type:    {data.Weapon}",
            $"Nation:         {data.Nation}",
            $"Rarity:         {data.Rarity} Stars",
            $"Portrait Cached:{(isCached ? "YES [HIT]" : "NO [MISS]")}",
            $"Local Asset:    {localPortrait}",
            $"Description:    {data.Description}"
        };

        ctx.Renderer.RenderBox($"REMOTE REST API QUERY: {data.Name.ToUpperInvariant()}", lines);
        return CommandResult.Ok($"Queried remote character {data.Name}.", data);
    }

    private static CommandResult ExecuteStatus(Args args, CommandExecutionContext ctx)
    {
        var savesCount = ctx.SaveManager.ListSaves().Count;
        var charsCount = ctx.Game.Characters.Count;
        var itemsCount = ctx.Game.Items.Count;
        var abCount = ctx.Game.Abilities.Count;
        var cacheStats = ctx.AssetCache.GetStatistics();

        var lines = new[]
        {
            $"Active Characters: {charsCount}",
            $"Inventory Items:   {itemsCount}",
            $"Grimoire Skills:   {abCount}",
            $"Saved Game Slots:  {savesCount}",
            $"Cached Portraits:  {cacheStats.TotalCachedFiles} ({cacheStats.TotalSizeBytes:N0} B, Hit Ratio: {cacheStats.HitRatio * 100:F0}%)",
            $"System Timestamp:  {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"
        };

        ctx.Renderer.RenderBox("SYSTEM DIAGNOSTICS & METRICS", lines);
        return CommandResult.Ok("Status displayed.");
    }
}
