using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IConsoleHelperService
{
    void WaitForKey();
    void RunSsh(ServerHost host);
}
