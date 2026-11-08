using Project.Infra.Display;
using Xunit;

namespace Tests.Doubles;

/// <summary>
/// [Testing Pattern: Mock / Spy]
/// A test spy and hand-rolled mock implementation of IDisplayer.
/// Records behavioral interactions (invocation count, exact parameters) to verify expected collaboration.
/// </summary>
public class MockDisplayer : IDisplayer
{
    private readonly List<string> _recordedLines = [];
    private readonly List<IDisplayable> _recordedDisplayables = [];

    public int StringDisplayCallCount { get; private set; }
    public int DtoDisplayCallCount { get; private set; }
    public int TotalCallCount => StringDisplayCallCount + DtoDisplayCallCount;

    public IReadOnlyList<string> RecordedLines => _recordedLines.AsReadOnly();
    public IReadOnlyList<IDisplayable> RecordedDisplayables => _recordedDisplayables.AsReadOnly();

    public void Display(string line)
    {
        StringDisplayCallCount++;
        _recordedLines.Add(line);
    }

    public void Display(IDisplayable displayable)
    {
        DtoDisplayCallCount++;
        _recordedDisplayables.Add(displayable);
        _recordedLines.Add(displayable.Render());
    }

    /// <summary>
    /// Verifies that Display was invoked at least once with the specified substring.
    /// </summary>
    public void VerifyContains(string snippet)
    {
        Assert.True(
            _recordedLines.Any(l => l.Contains(snippet, StringComparison.OrdinalIgnoreCase)),
            $"Expected output to contain '{snippet}', but recorded lines were:\n{string.Join("\n", _recordedLines)}");
    }

    /// <summary>
    /// Verifies the total number of Display invocations.
    /// </summary>
    public void VerifyTotalCalls(int expectedCount)
    {
        Assert.Equal(expectedCount, TotalCallCount);
    }

    /// <summary>
    /// Verifies that no output was emitted.
    /// </summary>
    public void VerifyNeverCalled()
    {
        Assert.Equal(0, TotalCallCount);
    }
}
