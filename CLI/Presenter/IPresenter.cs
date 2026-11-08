namespace Project.CLI.Presenter;

/// <summary>
/// Interface for presentation controller managing user interaction and execution loops.
/// </summary>
public interface IPresenter
{
    void Run(string[] args);
    string ReadLine(string prompt);
    void RunScript(string scriptFilePath);
}
