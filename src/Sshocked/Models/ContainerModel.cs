namespace Sshocked.Models;

public sealed class ContainerModel
{
    public string ContainerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Ports { get; set; }
    public string? ComposeProject { get; set; }
}
