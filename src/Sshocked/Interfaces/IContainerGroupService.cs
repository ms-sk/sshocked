using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IContainerGroupService
{
    Task ExecAllSequential(AppConfig config, List<ContainerModel> containers);
    Task ExecAllMultiTab(AppConfig config, List<ContainerModel> containers);
    Task RunCommandOnAll(AppConfig config, List<ContainerModel> containers, string command);
    ServerHost? FindParentServer(AppConfig config, ContainerModel container);
}
