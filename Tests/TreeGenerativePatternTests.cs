using Project.Core.Text.Builders;
using Project.Core.Text.Factories;
using Project.Core.Text.Models;
using Project.Core.Text.Prototypes;
using Project.Core.Text.Services;
using Tests.Doubles;
using Xunit;

namespace Tests;

/// <summary>
/// Unit tests validating the four Generative (Creational) Design Patterns implemented for the tree structure:
/// 1. Abstract Factory (PlainText, Markdown, HTML element factories)
/// 2. Builder (DocumentTreeBuilder & SectionBuilder fluent construction)
/// 3. Prototype (Deep cloning of composite headings and leaf elements)
/// 4. Singleton (ActiveDocumentContext thread-safe single active document)
/// Adheres strictly to the AAA (Arrange, Act, Assert) pattern.
/// </summary>
public class TreeGenerativePatternTests
{
    #region 1. Abstract Factory Tests
    [Fact]
    public void MarkdownElementFactory_ProducesMarkdownFormattedNodes()
    {
        // ==========================================
        // Arrange
        // ==========================================
        ITextElementFactory factory = new MarkdownElementFactory();

        // ==========================================
        // Act
        // ==========================================
        var heading = factory.CreateHeading(1, "Introduction", rank: 2);
        var link = factory.CreateLink(2, "Docs", "https://example.org");
        var image = factory.CreateImage(3, "Banner", "https://example.org/banner.png");

        // ==========================================
        // Assert: Correct Markdown syntax generated
        // ==========================================
        Assert.Equal("## Introduction", heading.Name);
        Assert.Equal("[Docs](https://example.org)", link.Name);
        Assert.Equal("![Banner](https://example.org/banner.png)", image.Name);
    }

    [Fact]
    public void HtmlElementFactory_ProducesSemanticHtmlNodes()
    {
        // ==========================================
        // Arrange
        // ==========================================
        ITextElementFactory factory = new HtmlElementFactory();

        // ==========================================
        // Act
        // ==========================================
        var heading = factory.CreateHeading(1, "Chapter 1", rank: 1);
        var paragraph = factory.CreateParagraph(2, "Content body text.");
        var link = factory.CreateLink(3, "Link", "https://example.org");

        // ==========================================
        // Assert: Semantic HTML tags generated
        // ==========================================
        Assert.Equal("<h1>Chapter 1</h1>", heading.Name);
        Assert.Equal("<p>Content body text.</p>", paragraph.Content);
        Assert.Equal("<a href=\"https://example.org\">Link</a>", link.Name);
    }
    #endregion

    #region 2. Builder Tests
    [Fact]
    public void DocumentTreeBuilder_ConstructsComplexNestedTree_WithUniqueIds()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var factory = new MarkdownElementFactory();

        // ==========================================
        // Act: Step-by-step fluent document assembly
        // ==========================================
        var root = new DocumentTreeBuilder("DesignGuide")
            .WithFactory(factory)
            .AddParagraph("Introductory paragraph.")
            .AddSection("Chapter 1", 1, sec =>
            {
                sec.AddParagraph("Section body.");
                sec.AddSubsection("Subsection 1.1", sub =>
                {
                    sub.AddLink("Reference", "https://example.org/ref");
                    sub.AddImage("Diagram", "https://example.org/diag.png");
                });
            })
            .Build();

        // ==========================================
        // Assert: Structural validation of the built composite
        // ==========================================
        Assert.Equal("DesignGuide", root.Name);
        Assert.Equal(2, root.Children.Count); // 1 paragraph + 1 chapter

        var chapter1 = Assert.IsType<Heading>(root.Children[1]);
        Assert.Equal("# Chapter 1", chapter1.Name);
        Assert.Equal(2, chapter1.Children.Count); // 1 paragraph + 1 subsection

        var subSection = Assert.IsType<Heading>(chapter1.Children[1]);
        Assert.Equal("## Subsection 1.1", subSection.Name);
        Assert.Equal(2, subSection.Children.Count); // 1 link + 1 image
    }
    #endregion

    #region 3. Prototype Tests
    [Fact]
    public void Heading_DeepClone_CreatesCompletelyIndependentSubtreeCopy()
    {
        // ==========================================
        // Arrange: Original composite heading with leaf children
        // ==========================================
        var original = new Heading(10, "OriginalSection", 1);
        original.AddChild(new Paragraph(11, "Original paragraph."));
        original.AddChild(new TextLink(12, "Link", "https://original.com"));

        // ==========================================
        // Act: Perform recursive deep clone
        // ==========================================
        var clone = (Heading)original.Clone(deep: true);

        // Mutate original to test memory independence
        original.AddChild(new Paragraph(13, "New paragraph added only to original."));

        // ==========================================
        // Assert: Clone is identical in initial content but independent in memory
        // ==========================================
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(2, clone.Children.Count);
        Assert.Equal(3, original.Children.Count);
        Assert.NotSame(original.Children[0], clone.Children[0]);
    }

    [Fact]
    public void SectionTemplateRegistry_ProvidesClonedInstancesOfRegisteredPrototypes()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var registry = new SectionTemplateRegistry();

        // ==========================================
        // Act: Retrieve cloned prototype instances
        // ==========================================
        var clonedMethodology1 = registry.CreateFromTemplate("methodology", "Applied Research Methodology");
        var clonedMethodology2 = registry.CreateFromTemplate("methodology", "Experimental Validation");

        // ==========================================
        // Assert: Distinct independent clones with customized titles
        // ==========================================
        Assert.NotSame(clonedMethodology1, clonedMethodology2);
        Assert.Equal("Applied Research Methodology", clonedMethodology1.Name);
        Assert.Equal("Experimental Validation", clonedMethodology2.Name);
        Assert.NotEmpty(clonedMethodology1.Children);
        Assert.NotEmpty(clonedMethodology2.Children);
    }
    #endregion

    #region 4. Singleton Tests
    [Fact]
    public void ActiveDocumentContext_MaintainsSingleGlobalInstance()
    {
        // ==========================================
        // Arrange & Act
        // ==========================================
        var instance1 = ActiveDocumentContext.Instance;
        var instance2 = ActiveDocumentContext.Instance;

        // ==========================================
        // Assert: Same reference in memory
        // ==========================================
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void ActiveDocumentContext_SetActiveDocument_UpdatesSharedStateGlobally()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var fakeDisplayer = new FakeDisplayer();
        var newManager = new TextDocumentManager(fakeDisplayer, "SingletonTestDoc");

        // ==========================================
        // Act
        // ==========================================
        ActiveDocumentContext.Instance.SetActiveDocument(newManager);
        var currentRoot = ActiveDocumentContext.Instance.CurrentRoot;

        // ==========================================
        // Assert: Global state updated to match new document
        // ==========================================
        Assert.Equal("SingletonTestDoc", currentRoot.Name);
        Assert.Same(newManager, ActiveDocumentContext.Instance.CurrentManager);
    }
    #endregion
}
