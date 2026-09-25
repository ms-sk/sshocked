namespace Sshocked.Models;

public class ServerHost
{
    public string Id { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public int Port { get; set; } = 22;
    public string GroupId { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public AuthType AuthType { get; set; } = AuthType.SshKey;
    public string? IdentityFile { get; set; }
    public string? CustomSshOptions { get; set; }
}
