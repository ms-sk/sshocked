namespace Sshocked.Models;

public sealed class ComposeStackModel
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ConfigFiles { get; set; }
}
