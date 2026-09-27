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
    private readonly IMenuFactory _menuFactory;
    private readonly INavigationService _nav;
    private readonly IGroupConnectionService _groupConnection;
    private readonly IProcessService _processService;
    private readonly ILogger<GroupMenuService> _logger;

    public GroupMenuService(
        IConfigRepository configRepository,
        IGroupManagementService groupManagement,
        IServerMenuService serverMenu,
        ITableRendererService tableRenderer,
        IHostSelectorService hostSelector,
        IConsoleHelperService consoleHelper,
        IMenuFactory menuFactory,
        INavigationService nav,
        IGroupConnectionService groupConnection,
        IProcessService processService,
        ILogger<GroupMenuService> logger)
    {
        _configRepository = configRepository;
        _groupManagement = groupManagement;
        _serverMenu = serverMenu;
        _tableRenderer = tableRenderer;
        _hostSelector = hostSelector;
        _consoleHelper = consoleHelper;
        _menuFactory = menuFactory;
        _nav = nav;
        _groupConnection = groupConnection;
        _processService = processService;
        _logger = logger;
    }

    public async Task Browse()
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
            await ShowGroupDetail(config, group);
        }
    }

    private async Task ShowGroupDetail(AppConfig config, ServerGroup group)
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

            var menuEntries = new List<MenuEntry>
            {
                new("Select server...", 'S', executeAsync: () => { SelectServerFromGroup(config, group); return Task.CompletedTask; })
            };

            if (groupHosts.Count > 0)
            {
                menuEntries.Add(new MenuEntry("Connect all", 'C', executeAsync: () => ConnectAll(groupHosts)));
                menuEntries.Add(new MenuEntry("Run command", 'R', executeAsync: () => RunCommandOnGroup(groupHosts)));
            }

            menuEntries.Add(new MenuEntry("Back", 'B', actionType: MenuActionType.Back));

            var shouldContinue = await _menuFactory.RunMenu("[bold yellow]Group Actions[/]", menuEntries);

            if (!shouldContinue)
            {
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

    private async Task RunCommandOnGroup(List<ServerHost> hosts)
    {
        AnsiConsole.MarkupLine("[grey]Interactive mode — type a command to run on all servers.[/]");
        AnsiConsole.MarkupLine("[grey]Leave empty or type [bold]exit[/] to quit.[/]");
        AnsiConsole.WriteLine();

        while (true)
        {
            Console.Write("$ ");
            var line = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(line))
            {
                break;
            }

            var trimmed = line.Trim();
            if (trimmed.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            AnsiConsole.WriteLine();

            foreach (var host in hosts)
            {
                var header = $"-- {host.Alias} ({host.User}@{host.HostName}) ";
                AnsiConsole.Markup($"[bold]{Markup.Escape(header)}[/]");
                Console.WriteLine(new string('-', Math.Max(1, 60 - header.Length)));

                try
                {
                    var output = await _processService.RunSshCommand(host, trimmed);
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
        }
    }

    private async Task ConnectAll(List<ServerHost> hosts)
    {
        if (_groupConnection.CanLaunchMultiTab)
        {
            var menuEntries = new List<MenuEntry>
            {
                new("Sequential (one after another)", 'S', executeAsync: () => _groupConnection.ConnectAllSequential(hosts)),
                new("Multi-Tab (new terminal windows)", 'M', executeAsync: () => _groupConnection.ConnectAllMultiTab(hosts)),
                new("Back", 'B', actionType: MenuActionType.Back)
            };

            await _menuFactory.RunMenu("[bold yellow]Connect all — Strategy[/]", menuEntries);
            return;
        }

        await _groupConnection.ConnectAllSequential(hosts);
    }
}
