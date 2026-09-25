using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IServerCrudService
{
    ServerHost? Add();
    void Edit(ServerHost host);
    bool Delete(ServerHost host);
}
