using Moq;
using Project.CLI.Commands;
using Project.Core.Characters.Services;
using Project.Core.Text.Services;
using Project.Infra.Remote;
using Tests.Doubles;
using Tests.Fixtures;
using Xunit;

namespace Tests;

/// <summary>
/// Unit tests for the CLI command layer interacting with the remote REST API module.
/// Tests query commands, character imports, and structured text rendering.
/// </summary>
public class ApiCliCommandTests(RemoteApiTestFixture fixture) : IClassFixture<RemoteApiTestFixture>
{
    private readonly RemoteApiTestFixture _fixture = fixture;

    [Fact]
    public void ApiCommand_ListCharacters_OutputsCatalogToDisplayer()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var fakeDisplayer = new FakeDisplayer();
        var stubRepo = new StubRepositoryService();
        var game = new Game(fakeDisplayer);
        var textManager = new TextDocumentManager(fakeDisplayer, "Doc");
        var context = new CommandContext(game, textManager, stubRepo, fakeDisplayer, _fixture.ApiClient);

        var registry = new CommandRegistry();
        ApiCommands.Register(registry, _ => "default");

        var cmd = ParsedCommand.Parse("api chars");

        // ==========================================
        // Act
        // ==========================================
        registry.Execute(cmd, context);

        // ==========================================
        // Assert
        // ==========================================
        Assert.True(fakeDisplayer.ContainsText("Remote Characters Catalog"));
        Assert.True(fakeDisplayer.ContainsText("raiden"));
        Assert.True(fakeDisplayer.ContainsText("zhongli"));
    }

    [Fact]
    public void ApiCommand_ListWeapons_OutputsCatalogToDisplayer()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var fakeDisplayer = new FakeDisplayer();
        var stubRepo = new StubRepositoryService();
        var game = new Game(fakeDisplayer);
        var textManager = new TextDocumentManager(fakeDisplayer, "Doc");
        var context = new CommandContext(game, textManager, stubRepo, fakeDisplayer, _fixture.ApiClient);

        var registry = new CommandRegistry();
        ApiCommands.Register(registry, _ => "default");

        var cmd = ParsedCommand.Parse("api weapons");

        // ==========================================
        // Act
        // ==========================================
        registry.Execute(cmd, context);

        // ==========================================
        // Assert
        // ==========================================
        Assert.True(fakeDisplayer.ContainsText("Remote Weapons Catalog"));
        Assert.True(fakeDisplayer.ContainsText("engulfing-lightning"));
    }

    [Fact]
    public void ApiCommand_ImportCharacterAndWeapon_CreatesVirtualCharacterAndRendersStructuredText()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var fakeDisplayer = new FakeDisplayer();
        var stubRepo = new StubRepositoryService();
        var game = new Game(fakeDisplayer);
        var textManager = new TextDocumentManager(fakeDisplayer, "Doc");
        var context = new CommandContext(game, textManager, stubRepo, fakeDisplayer, _fixture.ApiClient);

        var registry = new CommandRegistry();
        ApiCommands.Register(registry, _ => "default");

        var cmd = ParsedCommand.Parse("api import raiden engulfing-lightning");

        // ==========================================
        // Act
        // ==========================================
        registry.Execute(cmd, context);

        // ==========================================
        // Assert: Virtual character registered in Game domain with equipped weapon
        // ==========================================
        Assert.Single(game.Characters);
        var importedChar = game.Characters[0];
        Assert.Equal("Raiden Shogun", importedChar.Name);
        Assert.Single(importedChar.EquippedItems);
        Assert.Equal("Engulfing Lightning", importedChar.EquippedItems[0].Name);

        // Assert: Structured text profile rendered to console output
        Assert.True(fakeDisplayer.ContainsText("GENERATED STRUCTURED TEXT PROFILE"));
        Assert.True(fakeDisplayer.ContainsText("Raiden Shogun"));
        Assert.True(fakeDisplayer.ContainsText("Combat Attributes"));
        Assert.True(fakeDisplayer.ContainsText("Secret Art: Musou Shinsetsu"));
    }

    [Fact]
    public void FetchCommand_AliasExecutesImportCorrectly()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var fakeDisplayer = new FakeDisplayer();
        var stubRepo = new StubRepositoryService();
        var game = new Game(fakeDisplayer);
        var textManager = new TextDocumentManager(fakeDisplayer, "Doc");
        var context = new CommandContext(game, textManager, stubRepo, fakeDisplayer, _fixture.ApiClient);

        var registry = new CommandRegistry();
        ApiCommands.Register(registry, _ => "default");

        var cmd = ParsedCommand.Parse("fetch raiden");

        // ==========================================
        // Act
        // ==========================================
        registry.Execute(cmd, context);

        // ==========================================
        // Assert
        // ==========================================
        Assert.Single(game.Characters);
        Assert.Equal("Raiden Shogun", game.Characters[0].Name);
    }
}
