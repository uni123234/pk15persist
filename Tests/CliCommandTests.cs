using Project.CLI.Commands;
using Project.Core.Characters.Services;
using Project.Core.Text.Services;
using Tests.Doubles;
using Xunit;

namespace Tests;

/// <summary>
/// Unit test suite for CLI command parsing, dynamic registration, and command handlers.
/// Adheres strictly to the AAA (Arrange, Act, Assert) testing pattern and Black-box testing approach.
/// </summary>
public class CliCommandTests
{
    [Fact]
    public void ParsedCommand_ParsesArgumentsAndFlagsAccurately()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var rawInput = "act attack Aragorn Gandalf --id 42 --critical";

        // ==========================================
        // Act
        // ==========================================
        var cmd = ParsedCommand.Parse(rawInput);

        // ==========================================
        // Assert
        // ==========================================
        Assert.Equal("act", cmd.Name);
        Assert.Equal(["attack", "Aragorn", "Gandalf"], cmd.Arguments);
        Assert.Equal("42", cmd.GetOption("id"));
        Assert.True(cmd.HasOption("critical"));
    }

    [Fact]
    public void ParsedCommand_HandlesQuotedStringsCorrectly()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var rawInput = "add leaf paragraph \"This is a multi-word paragraph.\" --author \"John Doe\"";

        // ==========================================
        // Act
        // ==========================================
        var cmd = ParsedCommand.Parse(rawInput);

        // ==========================================
        // Assert
        // ==========================================
        Assert.Equal("add", cmd.Name);
        Assert.Contains("This is a multi-word paragraph.", cmd.Arguments);
        Assert.Equal("John Doe", cmd.GetOption("author"));
    }

    [Fact]
    public void CommandRegistry_DispatchesRegisteredCommandToHandler()
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
        var handlerInvoked = false;

        registry.Register("status", "Reports service status", "status", (c, ctx) =>
        {
            handlerInvoked = true;
            ctx.Displayer.Display("SERVICE_OK");
        });

        var cmd = ParsedCommand.Parse("status");

        // ==========================================
        // Act
        // ==========================================
        registry.Execute(cmd, context);

        // ==========================================
        // Assert
        // ==========================================
        Assert.True(handlerInvoked);
        Assert.Contains("SERVICE_OK", fakeDisplayer.Messages);
    }

    [Fact]
    public void CommandRegistry_UnknownCommand_DisplaysWarningWithoutCrashing()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var mockDisplayer = new MockDisplayer();
        var stubRepo = new StubRepositoryService();
        var game = new Game(mockDisplayer);
        var textManager = new TextDocumentManager(mockDisplayer, "Doc");
        var context = new CommandContext(game, textManager, stubRepo, mockDisplayer);

        var registry = new CommandRegistry();
        var cmd = ParsedCommand.Parse("unknown_command_xyz");

        // ==========================================
        // Act
        // ==========================================
        registry.Execute(cmd, context);

        // ==========================================
        // Assert
        // ==========================================
        mockDisplayer.VerifyContains("Unknown command");
    }

    [Fact]
    public void CharactersCommands_CreateAndInspect_ExecutesViaRegistry()
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

        var createCmd = ParsedCommand.Parse("create char Boromir 120 30 0.2");
        var lsCmd = ParsedCommand.Parse("ls char");

        // ==========================================
        // Act
        // ==========================================
        registry.Execute(createCmd, context);
        registry.Execute(lsCmd, context);

        // ==========================================
        // Assert
        // ==========================================
        Assert.Single(game.Characters);
        Assert.Equal("Boromir", game.Characters[0].Name);
        Assert.True(fakeDisplayer.ContainsText("Boromir"));
    }

    [Fact]
    public void TextCommands_NavigationAndAdd_ExecutesViaRegistry()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var fakeDisplayer = new FakeDisplayer();
        var stubRepo = new StubRepositoryService();
        var game = new Game(fakeDisplayer);
        var textManager = new TextDocumentManager(fakeDisplayer, "RootBook");
        var context = new CommandContext(game, textManager, stubRepo, fakeDisplayer);

        var registry = new CommandRegistry();
        TextCommands.Register(registry, _ => "default");

        var addSecCmd = ParsedCommand.Parse("add container IntroSection");
        var cdCmd = ParsedCommand.Parse("cd IntroSection");
        var pwdCmd = ParsedCommand.Parse("pwd");

        // ==========================================
        // Act
        // ==========================================
        registry.Execute(addSecCmd, context);
        registry.Execute(cdCmd, context);
        registry.Execute(pwdCmd, context);

        // ==========================================
        // Assert
        // ==========================================
        Assert.True(fakeDisplayer.ContainsText("/RootBook/IntroSection"));
    }
}
