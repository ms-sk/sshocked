using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IServerMenuService
{
    void SelectServer();
    void ShowServerActions(AppConfig config, ServerHost host);
}