using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IServerMenuService
{
    Task SelectServer();
    Task ShowServerActions(AppConfig config, ServerHost host);
}