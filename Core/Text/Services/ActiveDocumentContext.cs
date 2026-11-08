using Project.Core.Text.Models;
using Project.Infra.Display;

namespace Project.Core.Text.Services;

/// <summary>
/// [Creational Pattern: Singleton]
/// Thread-safe singleton managing the single active document root and its lifecycle across the system.
/// Protects system invariants by guaranteeing that only one active document tree is manipulated at a time.
/// </summary>
public sealed class ActiveDocumentContext
{
    private static readonly Lazy<ActiveDocumentContext> _lazyInstance =
        new(() => new ActiveDocumentContext(), isThreadSafe: true);

    private readonly object _lock = new();
    private TextDocumentManager _currentManager;

    public static ActiveDocumentContext Instance => _lazyInstance.Value;

    private ActiveDocumentContext()
    {
        _currentManager = new TextDocumentManager(new NullDisplayer(), "Active_Document");
    }

    public TextDocumentManager CurrentManager
    {
        get
        {
            lock (_lock)
            {
                return _currentManager;
            }
        }
    }

    public Root CurrentRoot
    {
        get
        {
            lock (_lock)
            {
                return _currentManager.Root;
            }
        }
    }

    public void SetActiveDocument(TextDocumentManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        lock (_lock)
        {
            _currentManager = manager;
        }
    }

    public void Reset(string documentTitle = "Active_Document", IDisplayer? displayer = null)
    {
        lock (_lock)
        {
            _currentManager = new TextDocumentManager(displayer ?? new NullDisplayer(), documentTitle);
        }
    }
}
