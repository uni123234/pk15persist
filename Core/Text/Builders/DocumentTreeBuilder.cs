using Project.Core.Text.Factories;
using Project.Core.Text.Models;

namespace Project.Core.Text.Builders;

/// <summary>
/// [Creational Pattern: Builder]
/// Fluent builder for constructing complex multi-level structured documents.
/// Encapsulates hierarchical assembly, auto-increments element IDs, and delegates node creation
/// to an optional ITextElementFactory.
/// </summary>
public class DocumentTreeBuilder
{
    private string _title;
    private ITextElementFactory _factory = new PlainTextElementFactory();
    private int _nextId = 1;
    private readonly List<ITextElement> _rootChildren = [];

    public DocumentTreeBuilder(string title = "Document")
    {
        _title = title;
    }

    public DocumentTreeBuilder WithFactory(ITextElementFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
        return this;
    }

    public DocumentTreeBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public DocumentTreeBuilder AddParagraph(string content)
    {
        _rootChildren.Add(_factory.CreateParagraph(_nextId++, content));
        return this;
    }

    public DocumentTreeBuilder AddLink(string text, string url)
    {
        _rootChildren.Add(_factory.CreateLink(_nextId++, text, url));
        return this;
    }

    public DocumentTreeBuilder AddImage(string altText, string sourceUrl)
    {
        _rootChildren.Add(_factory.CreateImage(_nextId++, altText, sourceUrl));
        return this;
    }

    public DocumentTreeBuilder AddSection(string name, int rank = 1, Action<SectionBuilder>? configure = null)
    {
        var sectionBuilder = new SectionBuilder(name, rank, _factory, () => _nextId++, parentId: 1);
        configure?.Invoke(sectionBuilder);
        _rootChildren.Add(sectionBuilder.Build());
        return this;
    }

    public Root Build()
    {
        return new Root(1, _title, 0, _rootChildren, null);
    }
}

/// <summary>
/// Sub-builder for nested sections and subsections within a structured document tree.
/// </summary>
public class SectionBuilder
{
    private readonly string _name;
    private readonly int _rank;
    private readonly ITextElementFactory _factory;
    private readonly Func<int> _idGenerator;
    private readonly int? _parentId;
    private readonly int _sectionId;
    private readonly List<ITextElement> _children = [];

    public SectionBuilder(string name, int rank, ITextElementFactory factory, Func<int> idGenerator, int? parentId)
    {
        _name = name;
        _rank = rank;
        _factory = factory;
        _idGenerator = idGenerator;
        _parentId = parentId;
        _sectionId = _idGenerator();
    }

    public SectionBuilder AddParagraph(string content)
    {
        _children.Add(_factory.CreateParagraph(_idGenerator(), content));
        return this;
    }

    public SectionBuilder AddLink(string text, string url)
    {
        _children.Add(_factory.CreateLink(_idGenerator(), text, url));
        return this;
    }

    public SectionBuilder AddImage(string altText, string sourceUrl)
    {
        _children.Add(_factory.CreateImage(_idGenerator(), altText, sourceUrl));
        return this;
    }

    public SectionBuilder AddSubsection(string name, Action<SectionBuilder>? configure = null)
    {
        var subBuilder = new SectionBuilder(name, _rank + 1, _factory, _idGenerator, parentId: _sectionId);
        configure?.Invoke(subBuilder);
        _children.Add(subBuilder.Build());
        return this;
    }

    public Heading Build()
    {
        var heading = _factory.CreateHeading(_sectionId, _name, _rank, _parentId);
        foreach (var child in _children)
        {
            heading.AddChild(child);
        }
        return heading;
    }
}
