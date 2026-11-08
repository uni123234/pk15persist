namespace Project.Infra.Display;

/// <summary>
/// Writes lines and displayable objects directly to standard console output.
/// </summary>
public class ConsoleDisplayer : IDisplayer
{
    public void Display(string line) => Console.WriteLine(line);

    public void Display(IDisplayable displayable) => Console.WriteLine(displayable.Render());
}

/// <summary>
/// Silent displayer that discards output, useful for headless execution or initial seeding.
/// </summary>
public class NullDisplayer : IDisplayer
{
    public void Display(string line) { }
    public void Display(IDisplayable displayable) { }
}
