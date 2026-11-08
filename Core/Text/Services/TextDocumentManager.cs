using Project.Core.Text.Models;
using Project.Infra.Data.Models;
using Project.Infra.Data.Serialization;
using Project.Infra.Display;
using Project.Infra.Display.DTOs;

namespace Project.Core.Text.Services;

/// <summary>
/// Domain manager for structured hierarchical text documents supporting tree navigation, leaf additions, and persistence.
/// </summary>
public class TextDocumentManager
{
    private IDisplayer _displayer;
    private Root _root;
    private Heading _current;
    private int _nextId = 1;

    public void SetDisplayer(IDisplayer newDisplayer) => _displayer = newDisplayer;

    public Root Root => _root;
    public Heading CurrentNode => _current;

    public TextDocumentManager(IDisplayer displayer, string documentTitle = "Document")
    {
        _displayer = displayer;
        _root = new Root(documentTitle) { Id = _nextId++ };
        _current = _root;
    }

    public string Pwd()
    {
        var path = new List<string>();
        var curr = _current;
        while (curr != null)
        {
            path.Insert(0, curr.Name);
            curr = curr.ParentId.HasValue ? FindHeadingById(_root, curr.ParentId.Value) : null;
        }

        return "/" + string.Join("/", path);
    }

    public bool Up()
    {
        if (!_current.ParentId.HasValue)
        {
            _displayer.Display("Already at root level.");
            return false;
        }

        var parent = FindHeadingById(_root, _current.ParentId.Value);
        if (parent != null)
        {
            _current = parent;
            _displayer.Display($"Moved up to: {Pwd()}");
            return true;
        }

        return false;
    }

    public bool Cd(string target)
    {
        if (string.IsNullOrWhiteSpace(target) || target == "/" || target.Equals("/" + _root.Name, StringComparison.OrdinalIgnoreCase))
        {
            _current = _root;
            _displayer.Display($"Navigated to root: {Pwd()}");
            return true;
        }

        if (target == "..")
        {
            return Up();
        }

        if (target.StartsWith("/"))
        {
            // Absolute navigation
            var segments = target.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            Heading searchNode = _root;
            var startIdx = segments.Length > 0 && segments[0].Equals(_root.Name, StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            for (var i = startIdx; i < segments.Length; i++)
            {
                var match = searchNode.Children.OfType<Heading>().FirstOrDefault(h => h.Name.Equals(segments[i], StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    _displayer.Display($"Path segment '{segments[i]}' not found.");
                    return false;
                }
                searchNode = match;
            }

            _current = searchNode;
            _displayer.Display($"Navigated to: {Pwd()}");
            return true;
        }

        // Relative navigation
        var directChild = _current.Children.OfType<Heading>().FirstOrDefault(h => h.Name.Equals(target, StringComparison.OrdinalIgnoreCase));
        if (directChild != null)
        {
            _current = directChild;
            _displayer.Display($"Navigated to: {Pwd()}");
            return true;
        }

        _displayer.Display($"Container '{target}' not found in current scope ({_current.Name}).");
        return false;
    }

    public bool CdById(int id)
    {
        var found = FindHeadingById(_root, id);
        if (found != null)
        {
            _current = found;
            _displayer.Display($"Navigated to ID {id}: {Pwd()}");
            return true;
        }

        _displayer.Display($"No heading container found with ID #{id}.");
        return false;
    }

    public Heading AddHeading(string name, int? rank = null)
    {
        var r = rank ?? _current.Rank + 1;
        var heading = new Heading(_nextId++, name, r, [], _current.Id);
        _current.AddChild(heading);
        _displayer.Display($"Added section: '{name}' [#{heading.Id}] under '{_current.Name}'");
        return heading;
    }

    public Paragraph AddParagraph(string content)
    {
        var p = new Paragraph(_nextId++, content);
        _current.AddChild(p);
        _displayer.Display($"Added paragraph [#{p.Id}] to '{_current.Name}'.");
        return p;
    }

    public TextLink AddLink(string text, string url)
    {
        var link = new TextLink(_nextId++, text, url);
        _current.AddChild(link);
        _displayer.Display($"Added link [#{link.Id}] [{text}]({url}).");
        return link;
    }

    public TextImage AddImage(string altText, string sourceUrl)
    {
        var img = new TextImage(_nextId++, altText, sourceUrl);
        _current.AddChild(img);
        _displayer.Display($"Added image [#{img.Id}] ![{altText}]({sourceUrl}).");
        return img;
    }

    public bool Remove(string? name = null, int? id = null)
    {
        if (id.HasValue)
        {
            if (_current.RemoveChild(id.Value))
            {
                _displayer.Display($"Removed element #{id.Value} from '{_current.Name}'.");
                return true;
            }

            _displayer.Display($"Element #{id.Value} not found in '{_current.Name}'.");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            var match = _current.Children.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                _current.Children.Remove(match);
                _displayer.Display($"Removed element '{match.Name}' [#{match.Id}] from '{_current.Name}'.");
                return true;
            }

            _displayer.Display($"Element named '{name}' not found in current container.");
            return false;
        }

        // If no name or id passed, remove current container and go up
        if (!_current.ParentId.HasValue)
        {
            _displayer.Display("Cannot remove root document container.");
            return false;
        }

        var parent = FindHeadingById(_root, _current.ParentId.Value);
        if (parent != null)
        {
            var removedName = _current.Name;
            parent.Children.Remove(_current);
            _current = parent;
            _displayer.Display($"Removed section '{removedName}' and moved up to '{_current.Name}'.");
            return true;
        }

        return false;
    }

    public void Print(bool whole = false, bool showId = false, bool tocOnly = false)
    {
        var targetNode = whole ? _root : _current;
        var dto = new HeadingDto(targetNode, showId, tocOnly);
        _displayer.Display(dto);
    }

    public TextSave ExportSave(string saveName)
    {
        var nodes = new List<TextNodeData>();
        FlattenTree(_root, null, nodes);

        return new TextSave
        {
            SaveName = saveName,
            LastModified = DateTime.UtcNow,
            TextRoot = _root,
            Nodes = nodes
        };
    }

    public void ImportSave(TextSave save)
    {
        if (save.Nodes != null && save.Nodes.Count > 0)
        {
            var rootData = save.Nodes.FirstOrDefault(n => n.ParentId == null && n.NodeType == "Root")
                           ?? save.Nodes.FirstOrDefault(n => n.ParentId == null)
                           ?? save.Nodes[0];

            _root = new Root(rootData.Name) { Id = rootData.Id, Rank = rootData.Rank };
            var map = new Dictionary<int, Heading> { [_root.Id] = _root };

            // Reconstruct headings first
            foreach (var node in save.Nodes.Where(n => n.Id != _root.Id && (n.NodeType == "Heading" || n.NodeType == "Root")))
            {
                var h = new Heading(node.Id, node.Name, node.Rank, [], node.ParentId);
                map[h.Id] = h;
            }

            // Link parent-child hierarchy
            foreach (var node in save.Nodes)
            {
                if (node.Id == _root.Id) continue;

                var parent = node.ParentId.HasValue && map.TryGetValue(node.ParentId.Value, out var p) ? p : _root;

                switch (node.NodeType)
                {
                    case "Heading":
                        if (map.TryGetValue(node.Id, out var h))
                        {
                            parent.AddChild(h);
                        }
                        break;
                    case "Paragraph":
                        parent.AddChild(new Paragraph(node.Id, node.Content));
                        break;
                    case "Link":
                        parent.AddChild(new TextLink(node.Id, node.Name, node.Content));
                        break;
                    case "Image":
                        parent.AddChild(new TextImage(node.Id, node.Name, node.Content));
                        break;
                }
            }

            _current = _root;
            _nextId = save.Nodes.Max(n => n.Id) + 1;
        }
        else if (save.TextRoot != null)
        {
            _root = save.TextRoot;
            _current = _root;
            _nextId = 100;
        }

        _displayer.Display($"Successfully restored document '{save.SaveName}' (Root: {_root.Name}).");
    }

    private static void FlattenTree(Heading node, int? parentId, List<TextNodeData> list)
    {
        var type = node is Root ? "Root" : "Heading";
        list.Add(new TextNodeData(node.Id, parentId, type, node.Name, "", node.Rank));

        foreach (var child in node.Children)
        {
            switch (child)
            {
                case Heading subHeading:
                    FlattenTree(subHeading, node.Id, list);
                    break;
                case Paragraph p:
                    list.Add(new TextNodeData(p.Id, node.Id, "Paragraph", p.Name, p.Content, node.Rank + 1));
                    break;
                case TextLink l:
                    list.Add(new TextNodeData(l.Id, node.Id, "Link", l.Text, l.Url, node.Rank + 1));
                    break;
                case TextImage img:
                    list.Add(new TextNodeData(img.Id, node.Id, "Image", img.AltText, img.SourceUrl, node.Rank + 1));
                    break;
            }
        }
    }

    private static Heading? FindHeadingById(Heading root, int id)
    {
        if (root.Id == id) return root;
        foreach (var child in root.Children.OfType<Heading>())
        {
            var found = FindHeadingById(child, id);
            if (found != null) return found;
        }
        return null;
    }
}
