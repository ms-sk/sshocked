using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface ITableRendererService
{
    void RenderServerTable(AppConfig config);
    void RenderGroup(string groupName, List<ServerHost> hosts);
    string GetAuthLabel(AuthType authType);
}
