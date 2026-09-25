using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface ISshRunnerService
{
    Task ConnectAsync(ServerHost host);
}
