using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IGroupConnectionService
{
    Task ConnectAllSequential(List<ServerHost> hosts);

    Task ConnectAllMultiTab(List<ServerHost> hosts);
}
