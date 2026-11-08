using Moq;
using Project.CLI.Commands;
using Project.Core.Characters.Models;
using Project.Core.Characters.Services;
using Project.Core.Text.Services;
using Project.Infra.Data.Repositories;
using Project.Infra.Data.Serialization;
using Project.Infra.Display;
using Xunit;

namespace Tests;

/// <summary>
/// [Testing Pattern: Dynamic Mocking with Moq]
/// Demonstrates the use of the Moq framework for automated behavioral verification,
/// matching, setup, and exception simulation in accordance with the course specification.
/// </summary>
public class MoqDemonstrationTests
{
    [Fact]
    public void Game_BattleRound_InvokesDisplayerWithCombatInformation()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var mockDisplayer = new Mock<IDisplayer>();
        var game = new Game(mockDisplayer.Object);

        var warrior = game.CreateCharacter("Warrior", 100, 25, 0.1);
        var rogue = game.CreateCharacter("Rogue", 80, 20, 0.05);

        // ==========================================
        // Act
        // ==========================================
        game.ExecuteAttack(warrior, rogue);

        // ==========================================
        // Assert
        // ==========================================
        // Verify that Display was invoked with a message describing the attack
        mockDisplayer.Verify(
            d => d.Display(It.Is<string>(msg => msg.Contains("Warrior") && msg.Contains("attacked"))),
            Times.Once,
            "Expected game to log the attack action through IDisplayer.");
    }

    [Fact]
    public void Game_ExportSave_DoesNotInvokeDisplayer()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var mockDisplayer = new Mock<IDisplayer>(MockBehavior.Strict);
        var game = new Game(mockDisplayer.Object);
        game.CreateCharacter("Archer", 90, 30, 0.15);

        // ==========================================
        // Act
        // ==========================================
        var save = game.ExportSave("Slot_Alpha");

        // ==========================================
        // Assert
        // ==========================================
        Assert.NotNull(save);
        Assert.Single(save.Characters!);
        // Strict mock verifies no unexpected calls occurred on IDisplayer
        mockDisplayer.Verify(d => d.Display(It.IsAny<string>()), Times.Never);
        mockDisplayer.Verify(d => d.Display(It.IsAny<IDisplayable>()), Times.Never);
    }

    [Fact]
    public void CliCommand_SaveCommand_PersistsGameStateViaRepositoryMock()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var mockDisplayer = new Mock<IDisplayer>();
        var mockRepo = new Mock<IRepositoryService>();

        var game = new Game(mockDisplayer.Object);
        var textManager = new TextDocumentManager(mockDisplayer.Object, "TestDoc");
        var context = new CommandContext(game, textManager, mockRepo.Object, mockDisplayer.Object);

        var registry = new CommandRegistry();
        CharactersCommands.Register(registry, _ => "default");

        var saveCmd = ParsedCommand.Parse("save Checkpoint_Mock_1");

        // ==========================================
        // Act
        // ==========================================
        registry.Execute(saveCmd, context);

        // ==========================================
        // Assert
        // ==========================================
        // Verify that repository SaveGameState was invoked with the exact slot name
        mockRepo.Verify(
            r => r.SaveGameState("Checkpoint_Mock_1", It.IsNotNull<CharacterSave>()),
            Times.Once,
            "Save command must invoke IRepositoryService.SaveGameState with the parsed slot name.");

        // Verify user feedback was displayed
        mockDisplayer.Verify(
            d => d.Display(It.Is<string>(s => s.Contains("Saved game state"))),
            Times.Once);
    }

    [Fact]
    public void CliCommand_LoadCommand_RestoresStateFromRepositoryMock()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var mockDisplayer = new Mock<IDisplayer>();
        var mockRepo = new Mock<IRepositoryService>();

        var cannedSave = new CharacterSave
        {
            SlotName = "RestoreSlot",
            Characters =
            [
                new Project.Infra.Data.Models.CharacterData
                {
                    Id = 1,
                    Name = "RestoredHero",
                    Health = 95,
                    BasePower = 40,
                    BaseResistance = 0.2
                }
            ],
            Items = [],
            Abilities = [],
            CharacterItems = [],
            CharacterAbilities = []
        };

        // Configure mock stubbing behavior
        mockRepo
            .Setup(r => r.LoadGameState("RestoreSlot"))
            .Returns(cannedSave);

        var game = new Game(mockDisplayer.Object);
        var textManager = new TextDocumentManager(mockDisplayer.Object, "TestDoc");
        var context = new CommandContext(game, textManager, mockRepo.Object, mockDisplayer.Object);

        var registry = new CommandRegistry();
        CharactersCommands.Register(registry, _ => "default");

        var loadCmd = ParsedCommand.Parse("load RestoreSlot");

        // ==========================================
        // Act
        // ==========================================
        registry.Execute(loadCmd, context);

        // ==========================================
        // Assert
        // ==========================================
        // Confirm repository method was called
        mockRepo.Verify(r => r.LoadGameState("RestoreSlot"), Times.Once);

        // Confirm game state was populated from restored entity
        Assert.Single(game.Characters);
        Assert.Equal("RestoredHero", game.Characters[0].Name);
        Assert.Equal(95, game.Characters[0].Health);
    }

    [Fact]
    public void TextDocumentManager_AddHeading_NotifiesDisplayerWhenRequested()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var mockDisplayer = new Mock<IDisplayer>();
        var manager = new TextDocumentManager(mockDisplayer.Object, "Guide");

        // ==========================================
        // Act
        // ==========================================
        manager.AddHeading("Chapter 1: Principles");
        manager.Print(whole: true);

        // ==========================================
        // Assert
        // ==========================================
        // Verify that Print rendered the heading through IDisplayer.Display(IDisplayable)
        mockDisplayer.Verify(
            d => d.Display(It.Is<IDisplayable>(dto => dto.Render().Contains("Chapter 1: Principles"))),
            Times.AtLeastOnce);
    }
}
