using Project.CLI.Commands;
using Project.Core.Characters.Services;
using Project.Core.Text.Services;
using Tests.Doubles;
using Xunit;

namespace Tests;

/// <summary>
/// Integration test suite executing full multi-step scenarios from start to finish.
/// Tests end-to-end user workflows, state changes, and recovery under the command pipeline.
/// </summary>
public class ScenarioIntegrationTests
{
    [Fact]
    public void CombatAndPersistenceScenario_ExecutesSuccessfullyEndToEnd()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var fakeDisplayer = new FakeDisplayer();
        var stubRepo = new StubRepositoryService();
        var game = new Game(fakeDisplayer);
        var textManager = new TextDocumentManager(fakeDisplayer, "Doc");
        var context = new CommandContext(game, textManager, stubRepo, fakeDisplayer);

        var registry = new CommandRegistry();
        CharactersCommands.Register(registry, _ => "default");

        // ==========================================
        // Act: Step 1 - Create combatants and equip gear
        // ==========================================
        registry.Execute(ParsedCommand.Parse("create char Legolas 110 32 0.25"), context);
        registry.Execute(ParsedCommand.Parse("create char Gimli 140 28 0.35"), context);
        registry.Execute(ParsedCommand.Parse("create item Elven_Bow 10 20"), context);
        registry.Execute(ParsedCommand.Parse("add Legolas Elven_Bow"), context);

        // Step 2 - Checkpoint game state before combat
        registry.Execute(ParsedCommand.Parse("save PreBattleCheckpoint"), context);

        // Step 3 - Simulate combat engagement
        registry.Execute(ParsedCommand.Parse("act attack Legolas Gimli"), context);
        registry.Execute(ParsedCommand.Parse("act attack Gimli Legolas"), context);

        var gimliHealthAfterRound1 = game.Characters.First(c => c.Name == "Gimli").Health;
        var legolasHealthAfterRound1 = game.Characters.First(c => c.Name == "Legolas").Health;

        // Step 4 - Restore from checkpoint
        registry.Execute(ParsedCommand.Parse("load PreBattleCheckpoint"), context);

        var gimliHealthRestored = game.Characters.First(c => c.Name == "Gimli").Health;
        var legolasHealthRestored = game.Characters.First(c => c.Name == "Legolas").Health;

        // ==========================================
        // Assert
        // ==========================================
        Assert.True(gimliHealthAfterRound1 < 140, "Gimli must have taken damage during combat.");
        Assert.True(legolasHealthAfterRound1 < 120, "Legolas must have taken damage during combat.");
        Assert.Equal(140, gimliHealthRestored);
        Assert.Equal(120, legolasHealthRestored);
    }

    [Fact]
    public void TextDocumentScenario_HierarchyManipulationAndRecovery_ExecutesEndToEnd()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var fakeDisplayer = new FakeDisplayer();
        var stubRepo = new StubRepositoryService();
        var game = new Game(fakeDisplayer);
        var textManager = new TextDocumentManager(fakeDisplayer, "SoftwareSpec");
        var context = new CommandContext(game, textManager, stubRepo, fakeDisplayer);

        var registry = new CommandRegistry();
        TextCommands.Register(registry, _ => "default");

        // ==========================================
        // Act: Step 1 - Build hierarchical structure
        // ==========================================
        registry.Execute(ParsedCommand.Parse("add container Requirements"), context);
        registry.Execute(ParsedCommand.Parse("cd Requirements"), context);
        registry.Execute(ParsedCommand.Parse("add leaf paragraph \"FR-1: System shall execute automated unit tests.\""), context);
        registry.Execute(ParsedCommand.Parse("cd /"), context);

        // Step 2 - Save document checkpoint
        registry.Execute(ParsedCommand.Parse("save Spec_V1"), context);

        // Step 3 - Mutate hierarchy by removing section
        registry.Execute(ParsedCommand.Parse("rm Requirements --force"), context);
        var childrenAfterDelete = textManager.Root.Children.Count;

        // Step 4 - Restore from saved checkpoint
        registry.Execute(ParsedCommand.Parse("load Spec_V1"), context);
        var childrenAfterRestore = textManager.Root.Children.Count;

        // ==========================================
        // Assert
        // ==========================================
        Assert.Equal(0, childrenAfterDelete);
        Assert.Equal(1, childrenAfterRestore);
        Assert.Equal("Requirements", textManager.Root.Children[0].Name);
    }
}
