using Spectre.Console;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class ConsoleHelperService(IProcessService processService, IDockerService dockerService) : IConsoleHelperService
{
    public void WaitForKey()
    {
        AnsiConsole.MarkupLine("Press any key to continue...");
        System.Console.ReadKey(true);
    }

    public async Task RunSsh(ServerHost host)
    {
        AnsiConsole.Console.Profile.Capabilities.Interactive = false;
        System.Console.ResetColor();

        await processService.RunSsh(host);

        System.Console.ResetColor();
        AnsiConsole.Console.Profile.Capabilities.Interactive = true;
    }

    public async Task RunDockerExec(ServerHost host, ContainerModel container)
    {
        EnsureSudoPassword(host);

        AnsiConsole.Console.Profile.Capabilities.Interactive = false;
        System.Console.ResetColor();

        await dockerService.Exec(host, container);

        System.Console.ResetColor();
        AnsiConsole.Console.Profile.Capabilities.Interactive = true;
    }

    public async Task RunDockerLogs(ServerHost host, ContainerModel container)
    {
        EnsureSudoPassword(host);

        AnsiConsole.Console.Profile.Capabilities.Interactive = false;
        System.Console.ResetColor();

        await dockerService.Logs(host, container);

        System.Console.ResetColor();
        AnsiConsole.Console.Profile.Capabilities.Interactive = true;
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
}
