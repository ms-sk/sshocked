using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IHostSelectorService
{
    ServerHost? SelectHost(List<ServerHost> hosts, string title);
}
