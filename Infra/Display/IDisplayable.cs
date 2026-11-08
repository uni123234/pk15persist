namespace Project.Infra.Display;

/// <summary>
/// Defines an object that can produce a string representation for display.
/// </summary>
public interface IDisplayable
{
    string Render();
}
