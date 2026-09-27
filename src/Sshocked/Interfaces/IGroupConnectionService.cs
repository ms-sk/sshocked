using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IGroupConnectionService
{
    /// <summary>
    /// Whether the current platform supports launching multiple terminal windows/tabs.
    /// </summary>
    bool CanLaunchMultiTab { get; }

    /// <summary>
    /// Connect to each host sequentially — the next SSH session starts after the previous one exits.
    /// </summary>
    Task ConnectAllSequential(List<ServerHost> hosts);

    /// <summary>
    /// Launch each host connection in a new Windows Terminal tab.
    /// Only available when <see cref="IsWindowsTerminal"/> is true.
    /// </summary>
    Task ConnectAllMultiTab(List<ServerHost> hosts);
}
