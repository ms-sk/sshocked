using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class ContainerGroupService(
    IProcessService processService,
    IConsoleHelperService consoleHelper,
    ILogger<ContainerGroupService> logger) : IContainerGroupService
{
    public ServerHost? FindParentServer(AppConfig config, ContainerModel container)
    {
        foreach (var host in config.Hosts)
        {
            if (host.SavedContainers.Any(c => c.Id == container.Id))
            {
                return host;
            }
        }
        return null;
    }

    public async Task ExecAllSequential(AppConfig config, List<ContainerModel> containers)
    {
        foreach (var container in containers)
        {
            var server = FindParentServer(config, container);
            if (server is null)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Parent server not found for container '{Markup.Escape(container.Alias ?? container.Name)}'[/]");
                continue;
            }

            AnsiConsole.MarkupLine($"Connecting to [cyan]{Markup.Escape(container.Alias ?? container.Name)}[/]...");
            await consoleHelper.RunDockerExec(server, container);
        }
    }

    public async Task ExecAllMultiTab(AppConfig config, List<ContainerModel> containers)
    {
        if (OperatingSystem.IsWindows())
        {
            await LaunchWindowsTerminalTabs(config, containers);
        }
        else if (OperatingSystem.IsLinux())
        {
            await LaunchLinuxTerminals(config, containers);
        }
        else if (OperatingSystem.IsMacOS())
        {
            await LaunchMacTerminals(config, containers);
        }

        AnsiConsole.MarkupLine(
            $"[green]\u2713[/] Launched [cyan]{containers.Count}[/] container connection{(containers.Count == 1 ? "" : "s")}.");
    }

    private async Task LaunchWindowsTerminalTabs(AppConfig config, List<ContainerModel> containers)
    {
        foreach (var container in containers)
        {
            var server = FindParentServer(config, container);
            if (server is null) continue;

            var sshArgs = ProcessService.BuildSshArgs(server);
            var sudo = BuildSudoPrefix(server.SudoPassword);
            var command = $"{sudo}docker exec -it {container.Name} sh";
            var fullArgs = $"{sshArgs} -t -- {command}";

            logger.LogInformation(
                "Launching new Windows Terminal tab for container {ContainerAlias}: ssh {Args}",
                container.Alias ?? container.Name, fullArgs);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "wt.exe",
                    Arguments = $"-w 0 nt ssh {fullArgs}",
                    UseShellExecute = false
                }
            };
            process.Start();
            await Task.Delay(500);
        }
    }

    private async Task LaunchLinuxTerminals(AppConfig config, List<ContainerModel> containers)
    {
        var terminalCmd = DetectLinuxTerminal();

        foreach (var container in containers)
        {
            var server = FindParentServer(config, container);
            if (server is null) continue;

            var sshArgs = ProcessService.BuildSshArgs(server);
            var sudo = BuildSudoPrefix(server.SudoPassword);
            var command = $"{sudo}docker exec -it {container.Name} sh";
            var fullArgs = $"{sshArgs} -t -- {command}";

            logger.LogInformation(
                "Launching new terminal for container {ContainerAlias}: {Cmd} ssh {Args}",
                container.Alias ?? container.Name, terminalCmd, fullArgs);

            var parts = terminalCmd.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var fileName = parts[0];
            var extraArgs = parts.Length > 1 ? parts[1] + " " : "";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = $"{extraArgs}ssh {fullArgs}",
                    UseShellExecute = false
                }
            };
            process.Start();
            await Task.Delay(500);
        }
    }

    private async Task LaunchMacTerminals(AppConfig config, List<ContainerModel> containers)
    {
        foreach (var container in containers)
        {
            var server = FindParentServer(config, container);
            if (server is null) continue;

            var sshArgs = ProcessService.BuildSshArgs(server);
            var sudo = BuildSudoPrefix(server.SudoPassword);
            var command = $"{sudo}docker exec -it {container.Name} sh";
            var fullArgs = $"{sshArgs} -t -- {command}";

            logger.LogInformation(
                "Launching new Terminal.app window for container {ContainerAlias}: ssh {Args}",
                container.Alias ?? container.Name, fullArgs);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"-a Terminal ssh {fullArgs}",
                    UseShellExecute = false
                }
            };
            process.Start();
            await Task.Delay(500);
        }
    }

    public async Task RunCommandOnAll(AppConfig config, List<ContainerModel> containers, string command)
    {
        foreach (var container in containers)
        {
            var server = FindParentServer(config, container);
            if (server is null)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Parent server not found for container '{Markup.Escape(container.Alias ?? container.Name)}'[/]");
                AnsiConsole.WriteLine();
                continue;
            }

            EnsureSudoPassword(server);

            var header = $"-- {container.Alias ?? container.Name} ({server.Alias}) ";
            AnsiConsole.Markup($"[bold]{Markup.Escape(header)}[/]");
            AnsiConsole.WriteLine(new string('-', Math.Max(1, 60 - header.Length)));

            try
            {
                var sudo = BuildSudoPrefix(server.SudoPassword);
                var dockerCommand = $"{sudo}docker exec {container.Name} {command}";
                var output = await processService.RunSshCommand(server, dockerCommand);
                Console.Write(output);

                if (!output.EndsWith('\n'))
                {
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
            }

            AnsiConsole.WriteLine();
        }

        await Task.CompletedTask;
    }

    private static string BuildSudoPrefix(string? sudoPassword)
    {
        if (string.IsNullOrEmpty(sudoPassword))
            return "sudo ";

        var escaped = sudoPassword.Replace("'", "'\\''");
        return $"echo '{escaped}' | sudo -S ";
    }

    private static void EnsureSudoPassword(ServerHost host)
    {
        if (string.IsNullOrEmpty(host.SudoPassword))
        {
            var needsSudo = AnsiConsole.Confirm("Does this host require [yellow]sudo[/] to run Docker commands?", false);
            if (needsSudo)
            {
                host.SudoPassword = AnsiConsole.Prompt(
                    new TextPrompt<string>("Enter [yellow]sudo password[/]:")
                        .Secret());
            }
        }
    }

    private static string DetectLinuxTerminal()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KONSOLE_VERSION")))
            return "konsole --new-tab -e";

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GNOME_TERMINAL_SCREEN")))
            return "gnome-terminal --";

        if (Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP")?.Contains("XFCE", StringComparison.OrdinalIgnoreCase) == true)
            return "xfce4-terminal -e";

        if (Environment.GetEnvironmentVariable("DESKTOP_SESSION")?.Contains("LXDE", StringComparison.OrdinalIgnoreCase) == true)
            return "lxterminal -e";

        return "x-terminal-emulator -e";
    }
}
