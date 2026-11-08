namespace Project.CLI.Core;

/// <summary>
/// Execution outcome of a CLI command with status code, message, and optional payload.
/// </summary>
public record CommandResult(bool Success, string Message, object? Data = null)
{
    public static CommandResult Ok(string message = "Command executed successfully.", object? data = null)
        => new(true, message, data);

    public static CommandResult Fail(string errorMessage)
        => new(false, errorMessage, null);
}
