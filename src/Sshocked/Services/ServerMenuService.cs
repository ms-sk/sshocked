using Microsoft.Extensions.Logging;
using Spectre.Console;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class ServerMenuService : IServerMenuService
{
    private readonly IConfigRepository _configRepository;
    private readonly IServerCrudService _serverCrud;
    private readonly ITableRendererService _tableRenderer;
    private readonly IHostSelectorService _hostSelector;
    private readonly IConsoleHelperService _consoleHelper;
    private readonly INavigationService _nav;
    private readonly ILogger<ServerMenuService> _logger;

    public ServerMenuService(
        IConfigRepository configRepository,
        IServerCrudService serverCrud,
        ITableRendererService tableRenderer,
        IHostSelectorService hostSelector,
        IConsoleHelperService consoleHelper,
        INavigationService nav,
        ILogger<ServerMenuService> logger)
    {
        _configRepository = configRepository;
        _serverCrud = serverCrud;
        _tableRenderer = tableRenderer;
        _hostSelector = hostSelector;
        _consoleHelper = consoleHelper;
        _nav = nav;
        _logger = logger;
    }

    public void SelectServer()
    {
        var config = _configRepository.Load();
        if (config.Hosts.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No servers configured.[/]");
            _consoleHelper.WaitForKey();
            return;
        }

        _nav.Push(ViewType.ServerDetail);

        while (_nav.Current == ViewType.ServerDetail)
        {
            config = _configRepository.Load();
            AnsiConsole.Clear();
            _tableRenderer.RenderServerTable(config);

            var host = _hostSelector.SelectHost(config.Hosts, "Select a [green]server[/]");
            if (host is null)
            {
                _nav.Pop();
                return;
            }

            ShowServerActions(config, host);
        }
    }

    public void ShowServerActions(AppConfig config, ServerHost host)
    {
        _nav.Push(ViewType.ServerDetail);

        while (_nav.Current == ViewType.ServerDetail)
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"[bold cyan]Server: {Markup.Escape(host.Alias)}[/]");
            AnsiConsole.MarkupLine($"  Host: {Markup.Escape(host.HostName)}  User: {Markup.Escape(host.User)}  Port: {host.Port}");
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Server Actions[/]")
                    .PageSize(10)
                    .AddChoices([
                        MenuLabels.Connect,
                        MenuLabels.Edit,
                        MenuLabels.Delete,
                        MenuLabels.Back
                    ]));

            switch (choice)
            {
                case var c when c == MenuLabels.Connect:
                    _consoleHelper.RunSsh(host);
                    break;
                case var c when c == MenuLabels.Edit:
                    _serverCrud.Edit(host);
                    AnsiConsole.MarkupLine($"[green]\u2713[/] Server '[cyan]{Markup.Escape(host.Alias)}[/]' updated.");
                    _consoleHelper.WaitForKey();
                    break;
                case var c when c == MenuLabels.Delete:
                    if (_serverCrud.Delete(host))
                    {
                        AnsiConsole.MarkupLine($"[green]\u2713[/] Server '[cyan]{Markup.Escape(host.Alias)}[/]' deleted.");
                        _consoleHelper.WaitForKey();
                        _nav.Pop();
                        return;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[grey]Deletion cancelled.[/]");
                        _consoleHelper.WaitForKey();
                    }
                    break;
                case var c when c == MenuLabels.Back:
                    _nav.Pop();
                    return;
            }
        }
    }
}
