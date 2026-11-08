using Project.CLI.Core;
using Project.CLI.Facade;
using Project.CLI.Rendering;
using Project.CLI.Strategies;
using Project.Core.Characters.Models;
using Project.Core.Characters.Services;
using Project.Infra.Display.DTOs;
using Tests.Doubles;
using Tests.Fixtures;
using Xunit;

namespace Tests;

/// <summary>
/// Unit test suite verifying the CLI Architecture:
/// - CliFacade and ICommandStrategy dynamic registration
/// - ArgsParser and typed argument retrieval
/// - CLIGraphic visual formatters (Tables, Progress Bars, Framed Boxes)
/// - Information, Transformative, and Infrastructure command execution
/// - IRenderer and IRenderable dependency inversion
/// </summary>
public class CliFacadeTests
{
    private static (CliFacade facade, BufferedRenderer renderer, Game game, StubRepositoryService repo) CreateTestFacade()
    {
        var renderer = new BufferedRenderer();
        var fakeDisplayer = new FakeDisplayer();
        var repo = new StubRepositoryService();
        var apiClient = RemoteApiTestFixture.CreateStubApiClient();
        var game = new Game(fakeDisplayer);

        // Seed sample characters
        game.CreateCharacter("Aragorn", 120, 35, 0.20);
        game.CreateCharacter("Legolas", 100, 30, 0.15);
        game.CreateItem("Elven_Bow", 10, 25);

        var facade = new CliFacade(game, repo, apiClient, renderer);
        return (facade, renderer, game, repo);
    }

    // =========================================================================
    // 1. ArgsParser Tests
    // =========================================================================
    [Fact]
    public void ArgsParser_ParsesQuotesOptionsAndFlagsAccurately()
    {
        // Arrange
        var raw = "create-char \"Gandalf the Grey\" 100 25 0.15 --avatar \"https://img.link/1.png\" --critical";

        // Act
        var args = ArgsParser.Parse(raw);

        // Assert
        Assert.Equal("create-char", args.CommandName);
        Assert.Equal("Gandalf the Grey", args.GetArg(0));
        Assert.Equal("100", args.GetArg(1));
        Assert.Equal("25", args.GetArg(2));
        Assert.Equal("0.15", args.GetArg(3));
        Assert.Equal("https://img.link/1.png", args.GetOption("avatar"));
        Assert.True(args.HasFlag("critical"));
        Assert.False(args.HasFlag("pierce"));
    }

    [Fact]
    public void ArgsParser_GenericOptionParser_ReturnsParsedOrFallback()
    {
        // Arrange
        var raw = "run --timeout 5000 --retries 3";
        var args = ArgsParser.Parse(raw);

        // Act & Assert
        Assert.Equal(5000, args.GetOption<int>("timeout", 1000));
        Assert.Equal(3, args.GetOption<int>("retries", 1));
        Assert.Equal(10, args.GetOption<int>("missing", 10)); // Default fallback
    }

    // =========================================================================
    // 2. CLIGraphic Formatting Tests
    // =========================================================================
    [Fact]
    public void CLIGraphic_FormatTable_ProducesBoxedAsciiTable()
    {
        // Arrange
        var headers = new[] { "ID", "Name", "Role" };
        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "1", "Alice", "Admin" },
            new[] { "2", "Bob", "Player" }
        };

        // Act
        var table = CLIGraphic.FormatTable(headers, rows);

        // Assert
        Assert.Contains("┌", table);
        Assert.Contains("ID", table);
        Assert.Contains("Name", table);
        Assert.Contains("Alice", table);
        Assert.Contains("Bob", table);
        Assert.Contains("└", table);
    }

    [Fact]
    public void CLIGraphic_FormatProgressBar_ComputesPercentAndFills()
    {
        // Arrange & Act
        var bar50 = CLIGraphic.FormatProgressBar(5, 10, "Loading Assets", barWidth: 10);

        // Assert
        Assert.Contains("50%", bar50);
        Assert.Contains("(5/10)", bar50);
        Assert.Contains("Loading Assets", bar50);
        Assert.Contains("█████░░░░░", bar50);
    }

    [Fact]
    public void CLIGraphic_FormatBox_WrapsLinesWithTitle()
    {
        // Arrange
        var lines = new[] { "Operation Status: Ok", "Rows affected: 42" };

        // Act
        var box = CLIGraphic.FormatBox("SUMMARY", lines);

        // Assert
        Assert.Contains("╔", box);
        Assert.Contains("[ SUMMARY ]", box);
        Assert.Contains("Operation Status: Ok", box);
        Assert.Contains("Rows affected: 42", box);
        Assert.Contains("╚", box);
    }

    // =========================================================================
    // 3. Information Strategy Tests
    // =========================================================================
    [Fact]
    public void InformationStrategy_CharsCommand_RendersTableOfCharacters()
    {
        // Arrange
        var (facade, renderer, _, _) = CreateTestFacade();

        // Act
        var result = facade.ExecCommand("chars");

        // Assert
        Assert.True(result.Success);
        Assert.True(renderer.ContainsText("Aragorn"));
        Assert.True(renderer.ContainsText("Legolas"));
        Assert.True(renderer.ContainsText("https://genshin.jmp.blue/characters/"));
    }

    [Fact]
    public void InformationStrategy_CharCommand_RendersDetailedCard()
    {
        // Arrange
        var (facade, renderer, _, _) = CreateTestFacade();

        // Act
        var result = facade.ExecCommand("char Aragorn");

        // Assert
        Assert.True(result.Success);
        Assert.True(renderer.ContainsText("CHARACTER PROFILE: ARAGORN"));
        Assert.True(renderer.ContainsText("Health: 120/120"));
        Assert.True(renderer.ContainsText("Power:  35"));
    }

    // =========================================================================
    // 4. Transformative Strategy Tests
    // =========================================================================
    [Fact]
    public void TransformativeStrategy_CreateChar_AddsCharacterToGame()
    {
        // Arrange
        var (facade, renderer, game, _) = CreateTestFacade();

        // Act
        var result = facade.ExecCommand("create-char Boromir 130 28 0.20");

        // Assert
        Assert.True(result.Success);
        var created = game.FindCharacter("Boromir");
        Assert.NotNull(created);
        Assert.Equal(130, created.Health);
        Assert.Equal(28, created.Power);
        Assert.True(renderer.ContainsText("successfully created"));
    }

    [Fact]
    public void TransformativeStrategy_AttackCommand_ExecutesCombatCycle()
    {
        // Arrange
        var (facade, renderer, game, _) = CreateTestFacade();
        var victim = game.FindCharacter("Legolas")!;
        var initialHp = victim.Health;

        // Act
        var result = facade.ExecCommand("attack Aragorn Legolas --critical");

        // Assert
        Assert.True(result.Success);
        Assert.True(victim.Health < initialHp, "Legolas must have taken damage.");
    }

    [Fact]
    public void TransformativeStrategy_EquipCommand_AssignsItemToCharacter()
    {
        // Arrange
        var (facade, _, game, _) = CreateTestFacade();
        var aragorn = game.FindCharacter("Aragorn")!;
        Assert.Empty(aragorn.ActiveItems);

        // Act
        var result = facade.ExecCommand("equip Aragorn Elven_Bow");

        // Assert
        Assert.True(result.Success);
        Assert.Single(aragorn.ActiveItems);
        Assert.Equal("Elven_Bow", aragorn.ActiveItems[0].Name);
    }

    // =========================================================================
    // 5. Infrastructure Strategy Tests
    // =========================================================================
    [Fact]
    public void InfrastructureStrategy_SaveAndLoad_PersistsAndRestoresState()
    {
        // Arrange
        var (facade, _, game, repo) = CreateTestFacade();
        facade.ExecCommand("create-char Gimli 150 40 0.30");

        // Act 1: Save
        var saveResult = facade.ExecCommand("save CheckpointAlpha");
        Assert.True(saveResult.Success);
        Assert.True(repo.GetGameSaveNames().Contains("CheckpointAlpha"));

        // Act 2: Mutate local state
        facade.ExecCommand("create-char Temporary 10 10 0.0");
        Assert.NotNull(game.FindCharacter("Temporary"));

        // Act 3: Load
        var loadResult = facade.ExecCommand("load CheckpointAlpha");

        // Assert 3
        Assert.True(loadResult.Success);
        Assert.NotNull(game.FindCharacter("Gimli"));
        Assert.Null(game.FindCharacter("Temporary"));
    }

    [Fact]
    public void InfrastructureStrategy_Status_RendersDiagnosticMetrics()
    {
        // Arrange
        var (facade, renderer, _, _) = CreateTestFacade();

        // Act
        var result = facade.ExecCommand("status");

        // Assert
        Assert.True(result.Success);
        Assert.True(renderer.ContainsText("SYSTEM DIAGNOSTICS & METRICS"));
        Assert.True(renderer.ContainsText("Active Characters: 2"));
    }

    // =========================================================================
    // 6. IRenderable & Dependency Inversion Tests
    // =========================================================================
    [Fact]
    public void CliFacade_DisplayRenderable_InvokesRendererStrategy()
    {
        // Arrange
        var (facade, renderer, game, _) = CreateTestFacade();
        var aragorn = game.FindCharacter("Aragorn")!;
        var dto = new CharacterDto(aragorn);

        // Act
        facade.Display(dto);

        // Assert
        Assert.True(renderer.ContainsText("#1 Aragorn"));
        Assert.True(renderer.ContainsText("120/120"));
    }

    // =========================================================================
    // 7. Global Help & Unknown Command Validation
    // =========================================================================
    [Fact]
    public void CliFacade_HelpCommand_ListsAllStrategiesAndCommands()
    {
        // Arrange
        var (facade, renderer, _, _) = CreateTestFacade();

        // Act
        var result = facade.ExecCommand("help");

        // Assert
        Assert.True(result.Success);
        Assert.True(renderer.ContainsText("Information Strategy"));
        Assert.True(renderer.ContainsText("Transformative Action Strategy"));
        Assert.True(renderer.ContainsText("Infrastructure System Strategy"));
        Assert.True(renderer.ContainsText("chars"));
        Assert.True(renderer.ContainsText("attack"));
        Assert.True(renderer.ContainsText("save"));
    }

    [Fact]
    public void CliFacade_UnknownCommand_ReturnsFailureGracefully()
    {
        // Arrange
        var (facade, renderer, _, _) = CreateTestFacade();

        // Act
        var result = facade.ExecCommand("nonexistent_command foo bar");

        // Assert
        Assert.False(result.Success);
        Assert.True(renderer.ContainsText("Unknown command 'nonexistent_command'"));
    }
}
