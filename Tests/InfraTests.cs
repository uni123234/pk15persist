using Project.Core.Characters.Models;
using Project.Core.Characters.Services;
using Project.Core.Text.Services;
using Project.Infra.Data.Repositories;
using Project.Infra.Display.DTOs;
using Tests.Doubles;
using Xunit;

namespace Tests;

/// <summary>
/// Unit test suite for the Infra layer: DTO formatting, JSON repository persistence, and error handling.
/// Adheres strictly to the AAA (Arrange, Act, Assert) testing pattern and Black-box testing approach.
/// </summary>
public class InfraTests
{
    [Fact]
    public void CharacterDto_RendersFormattedStatsAndEquipment()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var hero = new Character(1, "Paladin", 100, 30, 0.15);
        hero.AddItem(new Item(1, "Relic", 20, 10));
        hero.AddAbility(new Ability(1, "Smite", 50, 0, "Deals holy damage"));

        // ==========================================
        // Act
        // ==========================================
        var dto = new CharacterDto(hero);
        var output = dto.Render();

        // ==========================================
        // Assert
        // ==========================================
        Assert.Contains("Paladin", output);
        Assert.Contains("Relic", output);
        Assert.Contains("Smite", output);
    }

    [Fact]
    public void TextDtos_HeadingAndLeaves_RenderIndentedStructure()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var fakeDisplayer = new FakeDisplayer();
        var manager = new TextDocumentManager(fakeDisplayer, "RootDoc");
        manager.AddParagraph("Introductory paragraph.");
        manager.AddLink("Web Link", "https://example.org");

        // ==========================================
        // Act
        // ==========================================
        var dto = new HeadingDto(manager.Root, showId: true);
        var rendered = dto.Render();

        // ==========================================
        // Assert
        // ==========================================
        Assert.Contains("RootDoc", rendered);
        Assert.Contains("Introductory paragraph.", rendered);
        Assert.Contains("https://example.org", rendered);
    }

    [Fact]
    public void JsonRepository_SavesAndRestores_PolymorphicSnapshots()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var tempFile = Path.Combine(Path.GetTempPath(), $"repo_test_{Guid.NewGuid():N}.json");
        try
        {
            var fakeDisplayer = new FakeDisplayer();
            var repoService = new JsonRepositoryService(tempFile);

            var game = new Game(fakeDisplayer);
            var item = game.CreateItem("Amulet", 15, 20);
            var ability = game.CreateAbility("Flash", 30, 0, "Blinds enemies");
            var hero = game.CreateCharacter("Mage", 80, 40, 0.05, [ability], [item]);

            var gameSave = game.ExportSave("SaveSlot1");
            repoService.SaveGameState("SaveSlot1", gameSave);

            var textManager = new TextDocumentManager(fakeDisplayer, "Notes");
            textManager.AddHeading("Part 1");
            textManager.Cd("Part 1");
            textManager.AddParagraph("Sample note body.");
            textManager.Cd("/");

            var textSave = textManager.ExportSave("TextSlot1");
            repoService.SaveTextDocument("TextSlot1", textSave);

            // ==========================================
            // Act: Re-read into fresh repo service instance
            // ==========================================
            var freshRepo = new JsonRepositoryService(tempFile);
            var restoredGameSave = freshRepo.LoadGameState("SaveSlot1");
            var restoredTextSave = freshRepo.LoadTextDocument("TextSlot1");

            // ==========================================
            // Assert
            // ==========================================
            Assert.NotNull(restoredGameSave);
            Assert.Single(restoredGameSave.Characters!);
            Assert.Equal("Mage", restoredGameSave.Characters![0].Name);

            Assert.NotNull(restoredTextSave);
            Assert.Equal("Notes", restoredTextSave.Nodes![0].Name);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void StubRepository_SimulatedFailure_ThrowsExpectedException()
    {
        // ==========================================
        // Arrange: Configure Stub to trigger simulated failure
        // ==========================================
        var stubRepo = new StubRepositoryService { ThrowOnLoad = true };

        // ==========================================
        // Act & Assert
        // ==========================================
        Assert.Throws<InvalidOperationException>(() =>
        {
            stubRepo.LoadGameState("CorruptedSlot");
        });
    }
}
