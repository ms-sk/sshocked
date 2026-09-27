using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class GroupConnectionService(IConsoleHelperService consoleHelper, ILogger<GroupConnectionService> logger) : IGroupConnectionService
{
    public async Task ConnectAllSequential(List<ServerHost> hosts)
    {
        foreach (var host in hosts)
        {
            AnsiConsole.MarkupLine($"Connecting to [cyan]{Markup.Escape(host.Alias)}[/]...");
            await consoleHelper.RunSsh(host);
        }
    }

    public async Task ConnectAllMultiTab(List<ServerHost> hosts)
    {
        if (OperatingSystem.IsWindows())
        {
            await LaunchWindowsTerminalTabs(hosts);
        }
        else if (OperatingSystem.IsLinux())
        {
            await LaunchLinuxTerminals(hosts);
        }
        else if (OperatingSystem.IsMacOS())
        {
            await LaunchMacTerminals(hosts);
        }

        AnsiConsole.MarkupLine(
            $"[green]\u2713[/] Launched [cyan]{hosts.Count}[/] connection{(hosts.Count == 1 ? "" : "s")}.");
    }

    private async Task LaunchWindowsTerminalTabs(List<ServerHost> hosts)
    {
        foreach (var host in hosts)
        {
            var args = ProcessService.BuildSshArgs(host);
                    logger.LogInformation(
                "Launching new Windows Terminal tab for {Alias}: ssh {Args}",
                host.Alias, args);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "wt.exe",
                    Arguments = $"-w 0 nt ssh {args}",
                    UseShellExecute = false
                }
            };
            process.Start();
            await Task.Delay(500);
        }
    }

    private async Task LaunchLinuxTerminals(List<ServerHost> hosts)
    {
        var terminalCmd = DetectLinuxTerminal();

        foreach (var host in hosts)
        {
            var args = ProcessService.BuildSshArgs(host);
                    logger.LogInformation(
                "Launching new terminal for {Alias}: {Cmd} ssh {Args}",
                host.Alias, terminalCmd, args);

            var parts = terminalCmd.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var fileName = parts[0];
            var extraArgs = parts.Length > 1 ? parts[1] + " " : "";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = $"{extraArgs}ssh {args}",
                    UseShellExecute = false
                }
            };
            process.Start();
            await Task.Delay(500);
        }
    }

    private async Task LaunchMacTerminals(List<ServerHost> hosts)
    {
        foreach (var host in hosts)
        {
            var args = ProcessService.BuildSshArgs(host);
                    logger.LogInformation(
                "Launching new Terminal.app window for {Alias}: ssh {Args}",
                host.Alias, args);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"-a Terminal ssh {args}",
                    UseShellExecute = false
                }
            };
            process.Start();
            await Task.Delay(500);
        }
    }

    private static string DetectLinuxTerminal()
    {
        // KDE Konsole
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KONSOLE_VERSION")))
            return "konsole --new-tab -e";

        // GNOME Terminal
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GNOME_TERMINAL_SCREEN")))
            return "gnome-terminal --";

        // XFCE Terminal
        if (Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP")?.Contains("XFCE", StringComparison.OrdinalIgnoreCase) == true)
            return "xfce4-terminal -e";

        // LXDE / LXTerminal
        if (Environment.GetEnvironmentVariable("DESKTOP_SESSION")?.Contains("LXDE", StringComparison.OrdinalIgnoreCase) == true)
            return "lxterminal -e";

        // Fallback: x-terminal-emulator (Debian alternatives) or xterm
        return "x-terminal-emulator -e";
    }
}
