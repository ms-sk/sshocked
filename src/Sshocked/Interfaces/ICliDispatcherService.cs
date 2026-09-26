namespace Sshocked.Interfaces;

public interface ICliDispatcherService
{
    Task<bool> Dispatch(ParseResult parseResult);
}
