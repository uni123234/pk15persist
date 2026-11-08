using Project.Core.FileWorkspace.Canvas;
using Project.Core.FileWorkspace.FileSystem;
using Project.Core.FileWorkspace.Services;
using Project.Core.FileWorkspace.States;
using Tests.Doubles;
using Xunit;

namespace Tests;

/// <summary>
/// Unit test suite verifying the State Pattern, Virtual Canvas, and File Workspace lifecycle.
/// Covers mode transitions (Explorer -> Viewer -> Editor), navigation, viewing, and buffer editing.
/// </summary>
public class StatePatternTests
{
    private static (InMemoryFileSystemService fs, FakeConsoleCanvas canvas, WorkspaceContext ctx) CreateTestWorkspace()
    {
        var fs = new InMemoryFileSystemService();
        fs.CreateDirectory("/docs");
        fs.WriteAllText("/docs/readme.txt", "Line 1: Hello World\nLine 2: OOP State Pattern\nLine 3: Third Line");
        fs.WriteAllText("/docs/notes.md", "# Notes\nPractical Work 12");
        fs.ChangeDirectory("/docs");

        var canvas = new FakeConsoleCanvas();
        var initialState = new ExplorerState();
        var ctx = new WorkspaceContext(fs, canvas, initialState);

        return (fs, canvas, ctx);
    }

    // =========================================================================
    // 1. Explorer State Tests (Mode: EXPLORER)
    // =========================================================================
    [Fact]
    public void ExplorerState_InitialRender_DisplaysDirectoryEntries()
    {
        // Arrange
        var (fs, canvas, ctx) = CreateTestWorkspace();

        // Act
        ctx.Render();

        // Assert
        Assert.Equal("EXPLORER", ctx.CurrentState.ModeName);
        Assert.Equal("Console File Explorer", canvas.HeaderTitle);
        Assert.Equal("/docs", canvas.HeaderPath);
        Assert.True(canvas.ContainsInBody("readme.txt"));
        Assert.True(canvas.ContainsInBody("notes.md"));
        Assert.True(canvas.ContainsInBody(".."));
    }

    [Fact]
    public void ExplorerState_CdCommand_NavigatesToParentDirectory()
    {
        // Arrange
        var (fs, canvas, ctx) = CreateTestWorkspace();

        // Act
        ctx.ProcessInput("cd ..");

        // Assert
        Assert.Equal("/", fs.CurrentDirectory);
        Assert.Contains("Changed directory", ctx.LastStatusMessage);
    }

    [Fact]
    public void ExplorerState_TouchAndRmCommands_CreatesAndDeletesFile()
    {
        // Arrange
        var (fs, canvas, ctx) = CreateTestWorkspace();

        // Act 1: Touch
        ctx.ProcessInput("touch new_document.txt");

        // Assert 1
        Assert.True(fs.FileExists("/docs/new_document.txt"));
        Assert.Contains("created", ctx.LastStatusMessage, StringComparison.OrdinalIgnoreCase);

        // Act 2: Rm
        ctx.ProcessInput("rm new_document.txt");

        // Assert 2
        Assert.False(fs.FileExists("/docs/new_document.txt"));
        Assert.Contains("deleted", ctx.LastStatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExplorerState_ViewNonExistentFile_ShowsErrorWithoutChangingState()
    {
        // Arrange
        var (fs, canvas, ctx) = CreateTestWorkspace();

        // Act
        ctx.ProcessInput("view missing.txt");

        // Assert
        Assert.Equal("EXPLORER", ctx.CurrentState.ModeName);
        Assert.Contains("not found", ctx.LastStatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // 2. Viewer State Tests (Mode: VIEWER)
    // =========================================================================
    [Fact]
    public void ViewerState_TransitionFromExplorer_LoadsAndDisplaysFileContent()
    {
        // Arrange
        var (fs, canvas, ctx) = CreateTestWorkspace();

        // Act
        ctx.ProcessInput("view readme.txt");

        // Assert
        Assert.Equal("VIEWER", ctx.CurrentState.ModeName);
        Assert.Equal("File Viewer", canvas.HeaderTitle);
        Assert.True(canvas.ContainsInBody("Line 1: Hello World"));
        Assert.True(canvas.ContainsInBody("Line 2: OOP State Pattern"));
        Assert.True(canvas.ContainsInBody("Line 3: Third Line"));
    }

    [Fact]
    public void ViewerState_BackCommand_ReturnsToExplorerState()
    {
        // Arrange
        var (fs, canvas, ctx) = CreateTestWorkspace();
        ctx.ProcessInput("view readme.txt");
        Assert.Equal("VIEWER", ctx.CurrentState.ModeName);

        // Act
        ctx.ProcessInput("back");

        // Assert
        Assert.Equal("EXPLORER", ctx.CurrentState.ModeName);
        Assert.Null(ctx.ActiveFilePath);
    }

    [Fact]
    public void ViewerState_Pagination_ScrollsDownAndUp()
    {
        // Arrange: create file with 25 lines and page size of 10
        var (fs, canvas, ctx) = CreateTestWorkspace();
        ctx.ViewerPageSize = 10;
        var lines = Enumerable.Range(1, 25).Select(i => $"Row number {i}").ToList();
        fs.WriteAllText("/docs/big.txt", string.Join("\n", lines));

        ctx.ProcessInput("view big.txt");
        Assert.Equal(0, ctx.ViewerScrollOffset);

        // Act: Scroll down
        ctx.ProcessInput("next");

        // Assert: Offset advanced
        Assert.Equal(10, ctx.ViewerScrollOffset);

        // Act: Scroll up
        ctx.ProcessInput("prev");

        // Assert: Offset returned to top
        Assert.Equal(0, ctx.ViewerScrollOffset);
    }

    // =========================================================================
    // 3. Editor State Tests (Mode: EDITOR)
    // =========================================================================
    [Fact]
    public void EditorState_TransitionFromViewer_AllowsEditingAndMarkingDirty()
    {
        // Arrange
        var (fs, canvas, ctx) = CreateTestWorkspace();
        ctx.ProcessInput("view readme.txt");
        ctx.ProcessInput("edit");

        // Assert: Now in EDITOR mode
        Assert.Equal("EDITOR", ctx.CurrentState.ModeName);
        Assert.False(ctx.IsBufferDirty);
        Assert.Equal(3, ctx.EditorBuffer.Count);

        // Act: Append new line
        ctx.ProcessInput("add Line 4: Newly added line");

        // Assert
        Assert.True(ctx.IsBufferDirty);
        Assert.Equal(4, ctx.EditorBuffer.Count);
        Assert.Equal("Line 4: Newly added line", ctx.EditorBuffer[3]);
    }

    [Fact]
    public void EditorState_InsertDeleteAndReplaceOperations_ModifiesBufferCorrectly()
    {
        // Arrange
        var (fs, canvas, ctx) = CreateTestWorkspace();
        ctx.ProcessInput("edit notes.md");

        // Act 1: Insert at position 2
        ctx.ProcessInput("ins 2 Sub-Heading inserted");
        Assert.Equal("Sub-Heading inserted", ctx.EditorBuffer[1]);

        // Act 2: Replace position 1
        ctx.ProcessInput("set 1 # Updated Title");
        Assert.Equal("# Updated Title", ctx.EditorBuffer[0]);

        // Act 3: Delete position 2
        ctx.ProcessInput("del 2");
        Assert.Equal("Practical Work 12", ctx.EditorBuffer[1]);
        Assert.Equal(2, ctx.EditorBuffer.Count);
    }

    [Fact]
    public void EditorState_SaveCommand_FlushesBufferToFileAndClearsDirtyFlag()
    {
        // Arrange
        var (fs, canvas, ctx) = CreateTestWorkspace();
        ctx.ProcessInput("edit draft.txt");
        ctx.ProcessInput("add First line of draft");
        ctx.ProcessInput("add Second line of draft");
        Assert.True(ctx.IsBufferDirty);

        // Act
        ctx.ProcessInput("save");

        // Assert
        Assert.False(ctx.IsBufferDirty);
        Assert.True(fs.FileExists("/docs/draft.txt"));
        var diskContent = fs.ReadAllText("/docs/draft.txt");
        Assert.Contains("First line of draft", diskContent);
        Assert.Contains("Second line of draft", diskContent);
    }

    [Fact]
    public void EditorState_CloseWithUnsavedChanges_BlocksUnlessForced()
    {
        // Arrange
        var (fs, canvas, ctx) = CreateTestWorkspace();
        ctx.ProcessInput("edit draft.txt");
        ctx.ProcessInput("add Unsaved important text");

        // Act 1: Soft close
        ctx.ProcessInput("close");

        // Assert 1: Prevented exit
        Assert.Equal("EDITOR", ctx.CurrentState.ModeName);
        Assert.Contains("Unsaved changes", ctx.LastStatusMessage);

        // Act 2: Force close
        ctx.ProcessInput("close!");

        // Assert 2: Successful exit to EXPLORER
        Assert.Equal("EXPLORER", ctx.CurrentState.ModeName);
    }

    // =========================================================================
    // 4. Complete State Graph Traversal
    // =========================================================================
    [Fact]
    public void WorkspaceContext_FullLifecycleGraphTraversal_RecordsAllStateTransitions()
    {
        // Arrange
        var (fs, canvas, ctx) = CreateTestWorkspace();

        // 1. EXPLORER -> VIEWER
        ctx.ProcessInput("view readme.txt");
        Assert.Equal("VIEWER", ctx.CurrentState.ModeName);

        // 2. VIEWER -> EDITOR
        ctx.ProcessInput("edit");
        Assert.Equal("EDITOR", ctx.CurrentState.ModeName);

        // 3. EDITOR -> VIEWER
        ctx.ProcessInput("view");
        Assert.Equal("VIEWER", ctx.CurrentState.ModeName);

        // 4. VIEWER -> EXPLORER
        ctx.ProcessInput("back");
        Assert.Equal("EXPLORER", ctx.CurrentState.ModeName);

        // Assert history
        var expectedSequence = new[] { "EXPLORER", "VIEWER", "EDITOR", "VIEWER", "EXPLORER" };
        Assert.Equal(expectedSequence, ctx.StateTransitionHistory);
    }

    // =========================================================================
    // 5. Virtual Console Canvas Rendering Tests
    // =========================================================================
    [Fact]
    public void VirtualConsoleCanvas_RenderToString_FormatsStructuredSegmentsWithBorders()
    {
        // Arrange
        var canvas = new VirtualConsoleCanvas(width: 60, height: 12);
        canvas.SetHeader("Demo Title", "EXPLORER", "/usr/local");
        canvas.SetBody(["Line Alpha", "Line Beta"]);
        canvas.SetFooter("Controls: [q] quit", "Operation succeeded");

        // Act
        var rendered = canvas.RenderToString();

        // Assert: Contains structural elements
        Assert.Contains("╔", rendered);
        Assert.Contains("║ [Demo Title]", rendered);
        Assert.Contains("[MODE: EXPLORER]", rendered);
        Assert.Contains("Path: /usr/local", rendered);
        Assert.Contains("Line Alpha", rendered);
        Assert.Contains("Line Beta", rendered);
        Assert.Contains("Status: Operation succeeded", rendered);
        Assert.Contains("Controls: [q] quit", rendered);
        Assert.Contains("╚", rendered);
    }

    // =========================================================================
    // 6. WorkspaceEngine Automated Batch Execution
    // =========================================================================
    [Fact]
    public void WorkspaceEngine_ExecuteBatch_ExecutesSequentialScenarioToCompletion()
    {
        // Arrange
        var fs = new InMemoryFileSystemService();
        var canvas = new FakeConsoleCanvas();
        var engine = new WorkspaceEngine(fs, canvas);

        var script = new[]
        {
            "mkdir projects",
            "cd projects",
            "touch app.cs",
            "edit app.cs",
            "add // Main application",
            "add class Program { }",
            "save",
            "close",
            "exit"
        };

        // Act
        engine.ExecuteBatch(script);

        // Assert
        Assert.True(engine.Context.ShouldExit);
        Assert.True(fs.FileExists("/projects/app.cs"));
        Assert.Contains("class Program", fs.ReadAllText("/projects/app.cs"));
    }
}
