using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface ISshRunnerService
{
    Task Connect(ServerHost host);
}
