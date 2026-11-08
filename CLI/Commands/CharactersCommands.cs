using Project.Core.Characters.Strategies;

namespace Project.CLI.Commands;

/// <summary>
/// Sets up and registers commands dedicated to the Characters game subsystem.
/// </summary>
public static class CharactersCommands
{
    public static void Register(CommandRegistry registry, Func<string, string> promptReader)
    {
        // 1. create <char|item|ability>
        registry.Register(
            "create",
            "Creates a character, item, or ability via interactive dialog or arguments.",
            "create <char|item|ability> [name] [params...]",
            (cmd, ctx) =>
            {
                var type = cmd.Arguments.Count > 0 ? cmd.Arguments[0].ToLowerInvariant() : "";
                if (string.IsNullOrEmpty(type))
                {
                    ctx.Displayer.Display("Select creation type: 1) Character, 2) Item, 3) Ability");
                    var choice = promptReader("Choice (1-3): ");
                    type = choice switch { "1" => "char", "2" => "item", "3" => "ability", _ => choice.ToLowerInvariant() };
                }

                switch (type)
                {
                    case "char":
                    case "character":
                    {
                        var name = cmd.Arguments.Count > 1 ? cmd.Arguments[1] : promptReader("Character Name: ");
                        var hpStr = cmd.Arguments.Count > 2 ? cmd.Arguments[2] : promptReader("Health (default 100): ");
                        var pwrStr = cmd.Arguments.Count > 3 ? cmd.Arguments[3] : promptReader("Power (default 25): ");
                        var resStr = cmd.Arguments.Count > 4 ? cmd.Arguments[4] : promptReader("Resistance 0.0-1.0 (default 0.15): ");

                        var hp = int.TryParse(hpStr, out var h) ? h : 100;
                        var pwr = int.TryParse(pwrStr, out var p) ? p : 25;
                        var res = double.TryParse(resStr, out var r) ? r : 0.15;

                        ctx.Game.CreateCharacter(name, hp, pwr, res);
                        break;
                    }
                    case "item":
                    {
                        var name = cmd.Arguments.Count > 1 ? cmd.Arguments[1] : promptReader("Item Name: ");
                        var hpModStr = cmd.Arguments.Count > 2 ? cmd.Arguments[2] : promptReader("HP Modifier (e.g. 50): ");
                        var pwrModStr = cmd.Arguments.Count > 3 ? cmd.Arguments[3] : promptReader("Power Modifier (e.g. 15): ");

                        var hpMod = double.TryParse(hpModStr, out var hm) ? hm : 50;
                        var pwrMod = double.TryParse(pwrModStr, out var pm) ? pm : 15;

                        ctx.Game.CreateItem(name, hpMod, pwrMod);
                        break;
                    }
                    case "ability":
                    {
                        var name = cmd.Arguments.Count > 1 ? cmd.Arguments[1] : promptReader("Ability Name: ");
                        var dmgStr = cmd.Arguments.Count > 2 ? cmd.Arguments[2] : promptReader("Damage (default 30): ");
                        var healStr = cmd.Arguments.Count > 3 ? cmd.Arguments[3] : promptReader("Heal (default 0): ");
                        var desc = cmd.Arguments.Count > 4 ? cmd.Arguments[4] : promptReader("Description: ");

                        var dmg = double.TryParse(dmgStr, out var d) ? d : 30;
                        var heal = double.TryParse(healStr, out var hl) ? hl : 0;

                        ctx.Game.CreateAbility(name, dmg, heal, desc);
                        break;
                    }
                    default:
                        ctx.Displayer.Display($"Unknown creation type '{type}'. Valid types: char, item, ability.");
                        break;
                }
            });

        // 2. add [--char_id <id/name>] [--id <id/name>]
        registry.Register(
            "add",
            "Equips an item or assigns an ability to a character.",
            "add [--char_id <char>] [--id <item/ability>] or add <char> <item/ability>",
            (cmd, ctx) =>
            {
                var charTarget = cmd.GetOption("char_id")
                                 ?? (cmd.Arguments.Count > 0 ? cmd.Arguments[0] : null)
                                 ?? promptReader("Character ID or Name: ");

                var itemId = cmd.GetOption("id")
                             ?? (cmd.Arguments.Count > 1 ? cmd.Arguments[1] : null)
                             ?? promptReader("Item or Ability ID/Name to assign: ");

                // Try equipping item first, if not found try ability
                if (ctx.Game.FindItem(itemId) != null)
                {
                    ctx.Game.EquipItem(charTarget, itemId);
                }
                else if (ctx.Game.FindAbility(itemId) != null)
                {
                    ctx.Game.LearnAbility(charTarget, itemId);
                }
                else
                {
                    ctx.Displayer.Display($"Could not find an Item or Ability matching '{itemId}'.");
                }
            });

        // 3. act <attack|heal|ability> <actor> <target> [--id <ability_id>]
        registry.Register(
            "act",
            "Executes combat action (attack, heal, or ability) between characters.",
            "act <attack|heal|ability> <actor> <target> [--id <id>]",
            (cmd, ctx) =>
            {
                var actionType = cmd.Arguments.Count > 0 ? cmd.Arguments[0].ToLowerInvariant() : promptReader("Action (attack/heal/ability): ");
                var actor = cmd.Arguments.Count > 1 ? cmd.Arguments[1] : promptReader("Actor (name/id): ");
                var target = cmd.Arguments.Count > 2 ? cmd.Arguments[2] : promptReader("Target (name/id): ");

                switch (actionType)
                {
                    case "attack":
                    {
                        IAttackStrategy? strategy = null;
                        if (cmd.HasFlag("critical") || cmd.HasFlag("crit"))
                        {
                            strategy = new CriticalStrikeStrategy();
                        }
                        else if (cmd.HasFlag("pierce") || cmd.HasFlag("armor_piercing"))
                        {
                            strategy = new ArmorPiercingStrategy();
                        }

                        ctx.Game.Attack(actor, target, strategy);
                        break;
                    }
                    case "heal":
                        ctx.Game.Heal(actor, target);
                        break;
                    case "ability":
                    {
                        var abId = cmd.GetOption("id")
                                   ?? (cmd.Arguments.Count > 3 ? cmd.Arguments[3] : null)
                                   ?? promptReader("Ability ID or Name: ");
                        ctx.Game.UseAbility(actor, target, abId);
                        break;
                    }
                    default:
                        ctx.Displayer.Display($"Unknown action '{actionType}'. Valid actions: attack, heal, ability.");
                        break;
                }
            });

        // 4. ls <char|item|ability> [--id <id>]
        registry.Register(
            "ls",
            "Lists overview or details of characters, items, or abilities.",
            "ls <char|item|ability> [--id <id/name>]",
            (cmd, ctx) =>
            {
                var target = cmd.Arguments.Count > 0 ? cmd.Arguments[0].ToLowerInvariant() : "char";
                var specificId = cmd.GetOption("id") ?? (cmd.Arguments.Count > 1 ? cmd.Arguments[1] : null);

                switch (target)
                {
                    case "char":
                    case "character":
                    case "characters":
                        ctx.Game.ListCharacters(specificId);
                        break;
                    case "item":
                    case "items":
                        ctx.Game.ListItems(specificId);
                        break;
                    case "ability":
                    case "abilities":
                        ctx.Game.ListAbilities(specificId);
                        break;
                    default:
                        ctx.Displayer.Display($"Unknown category '{target}'. Valid options: char, item, ability.");
                        break;
                }
            });

        // 5. save [name]
        registry.Register(
            "save",
            "Saves current game state snapshot into the JSON repository.",
            "save [save_name]",
            (cmd, ctx) =>
            {
                var name = cmd.Arguments.Count > 0 ? cmd.Arguments[0] : $"QuickSave_{DateTime.Now:yyyyMMdd_HHmmss}";
                var save = ctx.Game.ExportSave(name);
                ctx.Repository.SaveGameState(name, save);
                ctx.Displayer.Display($"Game state successfully saved to repository as '{name}'.");
            });

        // 6. load <name>
        registry.Register(
            "load",
            "Loads a saved game state from the JSON repository.",
            "load [save_name]",
            (cmd, ctx) =>
            {
                var name = cmd.Arguments.Count > 0 ? cmd.Arguments[0] : null;
                if (string.IsNullOrEmpty(name))
                {
                    var saves = ctx.Repository.GetGameSaveNames().ToList();
                    if (saves.Count == 0)
                    {
                        ctx.Displayer.Display("No saved game states found in repository.");
                        return;
                    }
                    ctx.Displayer.Display("Available game saves: " + string.Join(", ", saves));
                    name = promptReader("Enter save name to load: ");
                }

                var loaded = ctx.Repository.LoadGameState(name);
                if (loaded == null)
                {
                    ctx.Displayer.Display($"Save '{name}' not found in repository.");
                    return;
                }

                ctx.Game.ImportSave(loaded);
            });

        // 7. checkpoint <save|restore|undo> <char_id/name> [name]
        registry.Register(
            "checkpoint",
            "Manages character state snapshots (Memento pattern: save, restore, undo).",
            "checkpoint <save|restore|undo> <char_id/name> [checkpoint_name]",
            (cmd, ctx) =>
            {
                var action = cmd.Arguments.Count > 0 ? cmd.Arguments[0].ToLowerInvariant() : promptReader("Action (save/restore/undo): ");
                var character = cmd.Arguments.Count > 1 ? cmd.Arguments[1] : promptReader("Character ID/Name: ");
                var cpName = cmd.Arguments.Count > 2 ? cmd.Arguments[2] : null;

                switch (action)
                {
                    case "save":
                    case "snapshot":
                        ctx.Game.CreateSnapshot(character, cpName);
                        break;
                    case "restore":
                    case "load":
                        ctx.Game.RestoreSnapshot(character, cpName);
                        break;
                    case "undo":
                        ctx.Game.RestoreSnapshot(character, null);
                        break;
                    default:
                        ctx.Displayer.Display($"Unknown checkpoint action '{action}'. Valid actions: save, restore, undo.");
                        break;
                }
            });

        // 8. stats
        registry.Register(
            "stats",
            "Displays combat statistics accumulated by the Observer pattern.",
            "stats",
            (cmd, ctx) =>
            {
                ctx.Displayer.Display("=== Combat Analytics (Observer) ===");
                var stats = ctx.Game.StatsObserver;
                ctx.Displayer.Display("-- Damage Dealt --");
                foreach (var (k, v) in stats.DamageDealt)
                {
                    ctx.Displayer.Display($"  {k,-15} : {v} dmg");
                }
                ctx.Displayer.Display("-- Damage Taken --");
                foreach (var (k, v) in stats.DamageTaken)
                {
                    ctx.Displayer.Display($"  {k,-15} : {v} dmg");
                }
                ctx.Displayer.Display("-- Kills --");
                foreach (var (k, v) in stats.Kills)
                {
                    ctx.Displayer.Display($"  {k,-15} : {v} kills");
                }
            });

        // 9. switch
        registry.Register(
            "switch",
            "Switches the active CLI mode to Text document management.",
            "switch",
            (cmd, ctx) =>
            {
                ctx.SwitchToMode = "text";
            });

        // 8. help
        registry.Register(
            "help",
            "Displays reference of available Characters mode commands.",
            "help",
            (cmd, ctx) =>
            {
                ctx.Displayer.Display("=== Characters Mode Commands ===");
                foreach (var c in registry.GetAllCommands())
                {
                    ctx.Displayer.Display($"  {c.Usage,-45} - {c.Description}");
                }
            });

        // 9. exit / quit
        Action<ParsedCommand, CommandContext> exitAction = (cmd, ctx) =>
        {
            ctx.ShouldExit = true;
            ctx.Displayer.Display("Exiting interactive session.");
        };
        registry.Register("exit", "Exits the application.", "exit", exitAction);
        registry.Register("quit", "Exits the application.", "quit", exitAction);
    }
}
