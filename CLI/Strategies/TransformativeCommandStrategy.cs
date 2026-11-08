using Project.CLI.Core;
using Project.Core.Characters.Strategies;

namespace Project.CLI.Strategies;

/// <summary>
/// Strategy 2: Transformative / Action commands.
/// Executes domain state modifications, combat cycles, character generation, and item equipment.
/// </summary>
public class TransformativeCommandStrategy : ICommandStrategy
{
    public string StrategyName => "Transformative Action Strategy";
    public string Category => "Action";
    public string Description => "State mutations, character creation, item equipping, and combat turns.";

    public IReadOnlyList<Command> GetCommands() =>
    [
        new DelegateCommand(
            "create-char",
            "Creates a new character via arguments or interactive dialog.",
            "create-char <name> [health] [power] [resistance] [--avatar <url>]",
            Category,
            ExecuteCreateCharacter,
            aliases: ["new-char", "add-char"]
        ),
        new DelegateCommand(
            "create-item",
            "Creates a new equipable item in the inventory catalog.",
            "create-item <name> <hp_bonus> <power_bonus>",
            Category,
            ExecuteCreateItem,
            aliases: ["new-item"]
        ),
        new DelegateCommand(
            "equip",
            "Equips an existing item onto a target character.",
            "equip <character> <item>",
            Category,
            ExecuteEquipItem
        ),
        new DelegateCommand(
            "learn",
            "Assigns an ability from the grimoire to a character.",
            "learn <character> <ability>",
            Category,
            ExecuteLearnAbility
        ),
        new DelegateCommand(
            "attack",
            "Executes combat attack round between two characters supporting tactical strategies.",
            "attack <attacker> <target> [--critical] [--pierce]",
            Category,
            ExecuteAttack
        ),
        new DelegateCommand(
            "heal",
            "Executes restorative healing action between characters.",
            "heal <healer> <target>",
            Category,
            ExecuteHeal
        ),
        new DelegateCommand(
            "cast",
            "Casts a learned ability or offensive spell from actor to target.",
            "cast <actor> <target> <ability>",
            Category,
            ExecuteCastAbility
        ),
        new DelegateCommand(
            "start-game",
            "Initializes an interactive combat scenario between two characters.",
            "start-game <fighter1> <fighter2>",
            Category,
            ExecuteStartGame
        ),
        new DelegateCommand(
            "duel",
            "Runs a full turn-based battle simulation between two fighters and records turn history.",
            "duel <fighter1> <fighter2>",
            Category,
            ExecuteDuel,
            aliases: ["turn-battle"]
        ),
        new DelegateCommand(
            "history",
            "Displays the formatted table of turns from the recent turn-based battle history.",
            "history",
            Category,
            (args, ctx) =>
            {
                var table = ctx.Game.History.FormatHistoryTable();
                ctx.Renderer.RenderRaw(table);
                return CommandResult.Ok("History rendered.");
            },
            aliases: ["turns", "log"]
        )
    ];

    private static CommandResult ExecuteCreateCharacter(Args args, CommandExecutionContext ctx)
    {
        var name = args.GetArg(0);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = ctx.PromptReader("Enter Character Name: ");
            if (string.IsNullOrWhiteSpace(name))
                return CommandResult.Fail("Character name cannot be empty.");
        }

        var hpStr = args.GetArg(1) ?? ctx.PromptReader("Base Health (default 100): ");
        var pwrStr = args.GetArg(2) ?? ctx.PromptReader("Base Power (default 25): ");
        var resStr = args.GetArg(3) ?? ctx.PromptReader("Armor Resistance 0.0-1.0 (default 0.15): ");

        var hp = int.TryParse(hpStr, out var h) ? h : 100;
        var pwr = int.TryParse(pwrStr, out var p) ? p : 25;
        var res = double.TryParse(resStr, out var r) ? r : 0.15;

        var created = ctx.Game.CreateCharacter(name, hp, pwr, res);
        ctx.Renderer.RenderStatus($"Character '{created.Name}' successfully created (HP: {created.Health}, Power: {created.Power}).", true);
        return CommandResult.Ok($"Created character {created.Name}.", created);
    }

    private static CommandResult ExecuteCreateItem(Args args, CommandExecutionContext ctx)
    {
        var name = args.GetArg(0) ?? ctx.PromptReader("Item Name: ");
        var hpStr = args.GetArg(1) ?? ctx.PromptReader("HP Modifier (e.g. 50): ");
        var pwrStr = args.GetArg(2) ?? ctx.PromptReader("Power Modifier (e.g. 20): ");

        var hp = double.TryParse(hpStr, out var h) ? h : 50;
        var pwr = double.TryParse(pwrStr, out var p) ? p : 20;

        var item = ctx.Game.CreateItem(name, hp, pwr);
        ctx.Renderer.RenderStatus($"Item '{item.Name}' registered (HP Mod: +{item.HpMod}, Power Mod: +{item.PowerMod}).", true);
        return CommandResult.Ok($"Created item {item.Name}.", item);
    }

    private static CommandResult ExecuteEquipItem(Args args, CommandExecutionContext ctx)
    {
        var charTarget = args.GetArg(0) ?? ctx.PromptReader("Character ID or Name: ");
        var itemTarget = args.GetArg(1) ?? ctx.PromptReader("Item ID or Name: ");

        var success = ctx.Game.EquipItem(charTarget, itemTarget);
        if (success)
        {
            ctx.Renderer.RenderStatus($"Equipped '{itemTarget}' onto '{charTarget}'.", true);
            return CommandResult.Ok($"Equipped {itemTarget} to {charTarget}.");
        }

        ctx.Renderer.RenderStatus($"Failed to equip '{itemTarget}' to '{charTarget}'.", false);
        return CommandResult.Fail($"Failed to equip {itemTarget}.");
    }

    private static CommandResult ExecuteLearnAbility(Args args, CommandExecutionContext ctx)
    {
        var charTarget = args.GetArg(0) ?? ctx.PromptReader("Character ID or Name: ");
        var abTarget = args.GetArg(1) ?? ctx.PromptReader("Ability ID or Name: ");

        var success = ctx.Game.LearnAbility(charTarget, abTarget);
        if (success)
        {
            ctx.Renderer.RenderStatus($"Character '{charTarget}' learned ability '{abTarget}'.", true);
            return CommandResult.Ok($"Learned ability {abTarget}.");
        }

        ctx.Renderer.RenderStatus($"Failed to learn ability '{abTarget}'.", false);
        return CommandResult.Fail($"Failed to learn ability {abTarget}.");
    }

    private static CommandResult ExecuteAttack(Args args, CommandExecutionContext ctx)
    {
        var attacker = args.GetArg(0) ?? ctx.PromptReader("Attacker (Name/ID): ");
        var target = args.GetArg(1) ?? ctx.PromptReader("Target (Name/ID): ");

        IAttackStrategy? strategy = null;
        if (args.HasFlag("critical") || args.HasFlag("crit"))
        {
            strategy = new CriticalStrikeStrategy();
        }
        else if (args.HasFlag("pierce") || args.HasFlag("armor_piercing"))
        {
            strategy = new ArmorPiercingStrategy();
        }

        ctx.Game.Attack(attacker, target, strategy);
        return CommandResult.Ok($"Attack executed from {attacker} against {target}.");
    }

    private static CommandResult ExecuteHeal(Args args, CommandExecutionContext ctx)
    {
        var healer = args.GetArg(0) ?? ctx.PromptReader("Healer (Name/ID): ");
        var target = args.GetArg(1) ?? ctx.PromptReader("Target (Name/ID): ");

        ctx.Game.Heal(healer, target);
        return CommandResult.Ok($"Heal cast from {healer} to {target}.");
    }

    private static CommandResult ExecuteCastAbility(Args args, CommandExecutionContext ctx)
    {
        var actor = args.GetArg(0) ?? ctx.PromptReader("Caster (Name/ID): ");
        var target = args.GetArg(1) ?? ctx.PromptReader("Target (Name/ID): ");
        var ability = args.GetArg(2) ?? ctx.PromptReader("Ability Name: ");

        ctx.Game.UseAbility(actor, target, ability);
        return CommandResult.Ok($"Casted {ability} on {target}.");
    }

    private static CommandResult ExecuteStartGame(Args args, CommandExecutionContext ctx)
    {
        var p1 = args.GetArg(0) ?? ctx.PromptReader("First Duelist (Name): ");
        var p2 = args.GetArg(1) ?? ctx.PromptReader("Second Duelist (Name): ");

        var c1 = ctx.Game.FindCharacter(p1);
        var c2 = ctx.Game.FindCharacter(p2);

        if (c1 == null || c2 == null)
        {
            ctx.Renderer.RenderStatus("Both duelists must exist in the catalog to begin.", false);
            return CommandResult.Fail("Duelists not found.");
        }

        ctx.Renderer.RenderBox(
            "DUEL ARENA INITIALIZED",
            [
                $"⚔️ Matchup: {c1.Name} (HP: {c1.Health}) vs {c2.Name} (HP: {c2.Health})",
                "Combat turns can now be executed using 'attack', 'heal', or 'cast'.",
                "State can be checkpointed via 'save' and reviewed via 'chars'."
            ]
        );

        return CommandResult.Ok($"Duel initialized between {c1.Name} and {c2.Name}.");
    }

    private static CommandResult ExecuteDuel(Args args, CommandExecutionContext ctx)
    {
        var p1 = args.GetArg(0) ?? ctx.PromptReader("First Duelist (Name): ");
        var p2 = args.GetArg(1) ?? ctx.PromptReader("Second Duelist (Name): ");

        try
        {
            var result = ctx.Game.StartTurnBasedDuel(p1, p2);
            ctx.Renderer.RenderStatus($"Duel finished! Winner: {result.Winner.Name} in {result.TotalRounds} rounds ({result.TotalTurns} turns).", true);
            return CommandResult.Ok($"Winner: {result.Winner.Name}", result);
        }
        catch (Exception ex)
        {
            ctx.Renderer.RenderStatus($"Duel error: {ex.Message}", false);
            return CommandResult.Fail(ex.Message);
        }
    }
}
