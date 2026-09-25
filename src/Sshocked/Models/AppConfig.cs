namespace Sshocked.Models;

public sealed class AppConfig
{
    public List<ServerGroup> Groups { get; set; } = [];
    public List<ServerHost> Hosts { get; set; } = [];
    public bool IsFirstStart { get; set; } = true;
}
