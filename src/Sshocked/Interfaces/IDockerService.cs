using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IDockerService
{
    Task<DockerHostInfo> Scan(ServerHost host);
    Task Exec(ServerHost host, ContainerModel container);
    Task Logs(ServerHost host, ContainerModel container);
}
