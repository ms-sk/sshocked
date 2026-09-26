using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IConsoleHelperService
{
    void WaitForKey();
    void RunSsh(ServerHost host);
    void RunDockerExec(ServerHost host, ContainerModel container);
    void RunDockerLogs(ServerHost host, ContainerModel container);
}
