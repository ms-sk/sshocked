using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IConsoleHelperService
{
    void WaitForKey();
    Task RunSsh(ServerHost host);
    Task RunDockerExec(ServerHost host, ContainerModel container);
    Task RunDockerLogs(ServerHost host, ContainerModel container);
}
