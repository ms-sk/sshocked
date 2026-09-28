using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IContainerService
{
    ContainerModel? Save(ServerHost serverHost, ContainerModel scannedContainer);
    void Edit(ServerHost serverHost, ContainerModel savedContainer);
    bool Delete(ServerHost serverHost, ContainerModel savedContainer);
    List<ContainerModel> GetSavedByGroup(AppConfig config, string groupId);
}
