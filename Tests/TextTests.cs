using Project.Core.Text.Services;
using Tests.Doubles;
using Xunit;

namespace Tests;

/// <summary>
/// Unit test suite for the Core.Text composite document hierarchy and navigation manager.
/// Adheres strictly to the AAA (Arrange, Act, Assert) testing pattern and Black-box testing approach.
/// </summary>
public class TextTests
{
    [Fact]
    public void DocumentTree_Navigation_CdAndUpWorkAccurately()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var fakeDisplayer = new FakeDisplayer();
        var manager = new TextDocumentManager(fakeDisplayer, "Book");

        // ==========================================
        // Act & Assert (Step-by-step navigation check)
        // ==========================================
        manager.AddHeading("Chapter 1");
        manager.Cd("Chapter 1");
        Assert.Equal("/Book/Chapter 1", manager.Pwd());

        manager.AddHeading("Section 1.1");
        manager.Cd("Section 1.1");
        Assert.Equal("/Book/Chapter 1/Section 1.1", manager.Pwd());

        manager.Up();
        Assert.Equal("/Book/Chapter 1", manager.Pwd());

        manager.Cd("/");
        Assert.Equal("/Book", manager.Pwd());
    }

    [Fact]
    public void DocumentTree_AddAndRemoveElements_ModifiesHierarchy()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var fakeDisplayer = new FakeDisplayer();
        var manager = new TextDocumentManager(fakeDisplayer, "Doc");

        // ==========================================
        // Act
        // ==========================================
        var paragraph = manager.AddParagraph("Hello World");
        var childCountAfterAdd = manager.CurrentNode.Children.Count;

        manager.Remove(id: paragraph.Id);
        var childCountAfterRemove = manager.CurrentNode.Children.Count;

        // ==========================================
        // Assert
        // ==========================================
        Assert.Equal(1, childCountAfterAdd);
        Assert.Equal(0, childCountAfterRemove);
    }

    [Fact]
    public void DocumentTree_AddLeaves_SupportsParagraphLinkAndImage()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var fakeDisplayer = new FakeDisplayer();
        var manager = new TextDocumentManager(fakeDisplayer, "MultimediaDoc");

        // ==========================================
        // Act
        // ==========================================
        var p = manager.AddParagraph("This is intro text.");
        var l = manager.AddLink("Documentation", "https://example.com/docs");
        var img = manager.AddImage("Architecture Diagram", "https://example.com/diagram.png");

        // ==========================================
        // Assert
        // ==========================================
        Assert.Equal(3, manager.CurrentNode.Children.Count);
        Assert.Contains(p, manager.CurrentNode.Children);
        Assert.Contains(l, manager.CurrentNode.Children);
        Assert.Contains(img, manager.CurrentNode.Children);
    }

    [Fact]
    public void DocumentTree_PrintWholeDocument_OutputsFullHierarchyToDisplayer()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var mockDisplayer = new MockDisplayer();
        var manager = new TextDocumentManager(mockDisplayer, "Manual");

        manager.AddHeading("Getting Started");
        manager.Cd("Getting Started");
        manager.AddParagraph("Installation instructions.");
        manager.Cd("/");

        // ==========================================
        // Act
        // ==========================================
        manager.Print(whole: true);

        // ==========================================
        // Assert
        // ==========================================
        mockDisplayer.VerifyContains("Getting Started");
        mockDisplayer.VerifyContains("Installation instructions.");
        Assert.True(mockDisplayer.TotalCallCount >= 1);
    }

    [Fact]
    public void DocumentTree_SnapshotExportAndImport_PreservesStructure()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var fakeDisplayer = new FakeDisplayer();
        var manager = new TextDocumentManager(fakeDisplayer, "OriginalDoc");
        manager.AddHeading("Part One");
        manager.Cd("Part One");
        manager.AddParagraph("Important point.");
        manager.Cd("/");

        // ==========================================
        // Act
        // ==========================================
        var snapshot = manager.ExportSave("Snapshot1");
        var restoredManager = new TextDocumentManager(fakeDisplayer, "RestoredDoc");
        restoredManager.ImportSave(snapshot);

        // ==========================================
        // Assert
        // ==========================================
        Assert.Equal("OriginalDoc", restoredManager.Root.Name);
        Assert.Single(restoredManager.Root.Children);
        Assert.Equal("Part One", restoredManager.Root.Children[0].Name);
    }
}
