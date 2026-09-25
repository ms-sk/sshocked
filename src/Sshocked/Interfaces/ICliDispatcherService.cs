namespace Sshocked.Interfaces;

public interface ICliDispatcherService
{
    /// <summary>
    /// Handles a parsed CLI command (direct connect, --list, --help, --version)
    /// and returns true if the application should exit after handling.
    /// </summary>
    Task<bool> DispatchAsync(ParseResult parseResult);
}
