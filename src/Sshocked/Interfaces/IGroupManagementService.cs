using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IGroupManagementService
{
    List<ServerGroup> GetAll();
    ServerGroup Create(string name);
    void Rename(string groupId, string newName);
    void Delete(string groupId);
    void AssignHost(string hostId, string groupId);
}
