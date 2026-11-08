namespace Project.Infra.Display;

/// <summary>
/// Defines an output mechanism capable of rendering text or displayable DTOs.
/// </summary>
public interface IDisplayer
{
    void Display(string line);
    void Display(IDisplayable displayable);
}
