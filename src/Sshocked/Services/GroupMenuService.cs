using Microsoft.Extensions.Logging;
using Spectre.Console;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class GroupMenuService : IGroupMenuService
{
    private readonly IConfigRepository _configRepository;
    private readonly IGroupManagementService _groupManagement;
    private readonly IServerMenuService _serverMenu;
    private readonly ITableRendererService _tableRenderer;
    private readonly IHostSelectorService _hostSelector;
    private readonly IConsoleHelperService _consoleHelper;
    private readonly INavigationService _nav;
    private readonly ILogger<GroupMenuService> _logger;

    public GroupMenuService(
        IConfigRepository configRepository,
        IGroupManagementService groupManagement,
        IServerMenuService serverMenu,
        ITableRendererService tableRenderer,
        IHostSelectorService hostSelector,
        IConsoleHelperService consoleHelper,
        INavigationService nav,
        ILogger<GroupMenuService> logger)
    {
        _configRepository = configRepository;
        _groupManagement = groupManagement;
        _serverMenu = serverMenu;
        _tableRenderer = tableRenderer;
        _hostSelector = hostSelector;
        _consoleHelper = consoleHelper;
        _nav = nav;
        _logger = logger;
    }

    public void Browse()
    {
        var groups = _groupManagement.GetAll();
        if (groups.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No groups configured. Create one first.[/]");
            _consoleHelper.WaitForKey();
            return;
        }

        _nav.Push(ViewType.GroupDetail);

        while (_nav.Current == ViewType.GroupDetail)
        {
            var config = _configRepository.Load();
            groups = _groupManagement.GetAll();
            AnsiConsole.Clear();

            var backLabel = MenuLabels.BackToMainMenu;
            var groupNames = groups.Select(g => g.Name).ToList();
            groupNames.Add(backLabel);

            var selectedName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Select a Group[/] ([grey]clear search for Back[/]):")
                    .PageSize(10)
                    .EnableSearch()
                    .AddChoices(groupNames));

            if (selectedName == backLabel)
            {
                _nav.Pop();
                return;
            }

            var group = groups.First(g => g.Name == selectedName);
            ShowGroupDetail(config, group);
        }
    }

    private void ShowGroupDetail(AppConfig config, ServerGroup group)
    {
        _nav.Push(ViewType.ServerDetail);

        while (_nav.Current == ViewType.ServerDetail)
        {
            config = _configRepository.Load();
            var groupHosts = config.Hosts.Where(h => h.GroupId == group.Id).ToList();

            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"[bold cyan]Group: {Markup.Escape(group.Name)}[/]");
            AnsiConsole.WriteLine();

            if (groupHosts.Count == 0)
            {
                AnsiConsole.MarkupLine("[grey]No servers in this group.[/]");
            }
            else
            {
                _tableRenderer.RenderGroup(group.Name, groupHosts);
            }

            var choices = new List<string> { MenuLabels.SelectServer };
            if (groupHosts.Count > 0)
            {
                choices.Add(MenuLabels.ConnectAll);
            }
            choices.Add(MenuLabels.Back);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Group Actions[/]")
                    .PageSize(10)
                    .AddChoices(choices));

            switch (choice)
            {
                case var c when c == MenuLabels.SelectServer:
                    SelectServerFromGroup(config, group);
                    break;
                case var c when c == MenuLabels.ConnectAll:
                    ConnectAll(groupHosts);
                    break;
                case var c when c == MenuLabels.Back:
                    _nav.Pop();
                    return;
            }
        }
    }

    private void SelectServerFromGroup(AppConfig config, ServerGroup? groupFilter)
    {
        var hosts = groupFilter is not null
            ? config.Hosts.Where(h => h.GroupId == groupFilter.Id).ToList()
            : config.Hosts;

        if (hosts.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No servers available.[/]");
            _consoleHelper.WaitForKey();
            return;
        }

        var host = _hostSelector.SelectHost(hosts, "Select a [green]server[/]");
        if (host is null)
        {
            return;
        }

        _serverMenu.ShowServerActions(config, host);
    }

    private void ConnectAll(List<ServerHost> hosts)
    {
        foreach (var host in hosts)
        {
            AnsiConsole.MarkupLine($"Connecting to [cyan]{Markup.Escape(host.Alias)}[/]...");
            _consoleHelper.RunSsh(host);
        }
    }
}
