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

    public void RunSsh(ServerHost host)
    {
        AnsiConsole.Console.Profile.Capabilities.Interactive = false;
        System.Console.ResetColor();

        _processService.RunSsh(host).GetAwaiter().GetResult();

        System.Console.ResetColor();
        AnsiConsole.Console.Profile.Capabilities.Interactive = true;
    }

    public void RunDockerExec(ServerHost host, ContainerModel container)
    {
        AnsiConsole.Console.Profile.Capabilities.Interactive = false;
        System.Console.ResetColor();

        _dockerService.Exec(host, container).GetAwaiter().GetResult();

        System.Console.ResetColor();
        AnsiConsole.Console.Profile.Capabilities.Interactive = true;
    }

    public void RunDockerLogs(ServerHost host, ContainerModel container)
    {
        AnsiConsole.Console.Profile.Capabilities.Interactive = false;
        System.Console.ResetColor();

        _dockerService.Logs(host, container).GetAwaiter().GetResult();

        System.Console.ResetColor();
        AnsiConsole.Console.Profile.Capabilities.Interactive = true;
    }
}
