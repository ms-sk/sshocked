using Spectre.Console;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class ConsoleHelperService : IConsoleHelperService
{
    private readonly IProcessService _processService;

    public ConsoleHelperService(IProcessService processService)
    {
        _processService = processService;
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

        _processService.RunSshAsync(host).GetAwaiter().GetResult();

        System.Console.ResetColor();
        AnsiConsole.Console.Profile.Capabilities.Interactive = true;
    }
}
