using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IProcessService
{
    Task RunSsh(ServerHost host);
    Task<string> RunSshCommand(ServerHost host, string command);
    Task RunSshInteractive(ServerHost host, string command);
}