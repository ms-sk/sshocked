using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IProcessService
{
    Task RunSshAsync(ServerHost host);
}