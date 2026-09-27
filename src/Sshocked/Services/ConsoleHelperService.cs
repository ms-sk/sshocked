using Spectre.Console;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class ConsoleHelperService : IConsoleHelperService
{
    private readonly IProcessService _processService;
    private readonly IDockerService _dockerService;

    public ConsoleHelperService(IProcessService processService, IDockerService dockerService)
    {
        _processService = processService;
        _dockerService = dockerService;
    }

    public void WaitForKey()
    {
        AnsiConsole.MarkupLine("Press any key to continue...");
        System.Console.ReadKey(true);
    }

    public async Task RunSsh(ServerHost host)
    {
        AnsiConsole.Console.Profile.Capabilities.Interactive = false;
        System.Console.ResetColor();

        await _processService.RunSsh(host);

        System.Console.ResetColor();
        AnsiConsole.Console.Profile.Capabilities.Interactive = true;
    }

    public async Task RunDockerExec(ServerHost host, ContainerModel container)
    {
        AnsiConsole.Console.Profile.Capabilities.Interactive = false;
        System.Console.ResetColor();

        await _dockerService.Exec(host, container);

        System.Console.ResetColor();
        AnsiConsole.Console.Profile.Capabilities.Interactive = true;
    }

    public async Task RunDockerLogs(ServerHost host, ContainerModel container)
    {
        AnsiConsole.Console.Profile.Capabilities.Interactive = false;
        System.Console.ResetColor();

        await _dockerService.Logs(host, container);

        System.Console.ResetColor();
        AnsiConsole.Console.Profile.Capabilities.Interactive = true;
    }
}
