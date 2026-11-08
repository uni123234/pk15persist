using Project.Infra.Display;

namespace Tests.Doubles;

/// <summary>
/// [Testing Pattern: Fake]
/// A lightweight in-memory implementation of IDisplayer that captures output without performing any I/O operations.
/// Used to satisfy dependencies in tests while allowing state inspection.
/// </summary>
public class FakeDisplayer : IDisplayer
{
    private readonly List<string> _messages = [];
    private readonly List<IDisplayable> _displayables = [];

    public IReadOnlyList<string> Messages => _messages.AsReadOnly();
    public IReadOnlyList<IDisplayable> Displayables => _displayables.AsReadOnly();

    public string? LastMessage => _messages.Count > 0 ? _messages[^1] : null;

    public void Display(string line)
    {
        _messages.Add(line);
    }

    public void Display(IDisplayable displayable)
    {
        _displayables.Add(displayable);
        _messages.Add(displayable.Render());
    }

    public void Clear()
    {
        _messages.Clear();
        _displayables.Clear();
    }

    public bool ContainsText(string substring)
    {
        return _messages.Any(m => m.Contains(substring, StringComparison.OrdinalIgnoreCase));
    }
}
