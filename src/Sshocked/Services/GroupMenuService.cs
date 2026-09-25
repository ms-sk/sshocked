using Microsoft.Extensions.Logging;
using Spectre.Console;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public class GroupMenuService : IGroupMenuService
{
    private readonly IConfigRepository _configRepository;
    private readonly IGroupManagementService _groupManagement;
    private readonly IServerMenuService _serverMenu;
    private readonly IProcessService _processService;
    private readonly INavigationService _nav;
    private readonly ILogger<GroupMenuService> _logger;

    public GroupMenuService(
        IConfigRepository configRepository,
        IGroupManagementService groupManagement,
        IServerMenuService serverMenu,
        IProcessService processService,
        INavigationService nav,
        ILogger<GroupMenuService> logger)
    {
        _configRepository = configRepository;
        _groupManagement = groupManagement;
        _serverMenu = serverMenu;
        _processService = processService;
        _nav = nav;
        _logger = logger;
    }

    public void Browse()
    {
        var groups = _groupManagement.GetAll();
        if (groups.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No groups configured. Create one first.[/]");
            WaitForKey();
            return;
        }

        _nav.Push(ViewType.GroupDetail);

        while (_nav.Current == ViewType.GroupDetail)
        {
            var config = _configRepository.Load();
            groups = _groupManagement.GetAll();
            AnsiConsole.Clear();

            var groupNames = groups.Select(g => g.Name).ToList();

            var selectedName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Select a Group[/] ([grey]Esc[/] to go back)")
                    .PageSize(10)
                    .EnableSearch()
                    .AddChoices(groupNames));

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
                RenderGroup(group.Name, groupHosts);
            }

            var choices = new List<string> { "[[S]] Select server..." };
            if (groupHosts.Count > 0)
            {
                choices.Add("[[C]] Connect all");
            }
            choices.Add("[[B]] Back");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Group Actions[/]")
                    .PageSize(10)
                    .AddChoices(choices));

            switch (choice)
            {
                case "[[S]] Select server...":
                    SelectServerFromGroup(config, group);
                    break;
                case "[[C]] Connect all":
                    ConnectAll(groupHosts);
                    break;
                case "[[B]] Back":
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
            WaitForKey();
            return;
        }

        var hostLabels = hosts
            .Select(h => (Host: h, Label: $"{Markup.Escape(h.Alias)} ({Markup.Escape(h.HostName)})"))
            .ToList();

        var selectedLabel = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [green]server[/]:")
                .PageSize(10)
                .EnableSearch()
                .AddChoices(hostLabels.Select(hl => hl.Label)));

        var host = hostLabels.First(hl => hl.Label == selectedLabel).Host;
        _serverMenu.ShowServerActions(config, host);
    }

    private void ConnectAll(List<ServerHost> hosts)
    {
        foreach (var host in hosts)
        {
            AnsiConsole.MarkupLine($"Connecting to [cyan]{Markup.Escape(host.Alias)}[/]...");
            RunSsh(host);
        }
    }

    private void RunSsh(ServerHost host)
    {
        AnsiConsole.Console.Profile.Capabilities.Interactive = false;
        System.Console.ResetColor();

        _processService.RunSshAsync(host).GetAwaiter().GetResult();

        System.Console.ResetColor();
        AnsiConsole.Console.Profile.Capabilities.Interactive = true;
    }

    private static void RenderGroup(string groupName, List<ServerHost> hosts)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[cyan]{groupName}[/]")
            .AddColumns([
                new TableColumn("Alias").Centered(),
                new TableColumn("Host").Centered(),
                new TableColumn("User").Centered(),
                new TableColumn("Port").Centered(),
                new TableColumn("Auth").Centered()
            ]);

        foreach (var host in hosts)
        {
            var authLabel = host.AuthType switch
            {
                AuthType.Password => "Password",
                AuthType.SshAgent => "Agent",
                AuthType.CustomConfig => "Custom",
                _ => "SSH Key"
            };

            table.AddRow(
                new Markup($"[bold]{Markup.Escape(host.Alias)}[/]"),
                new Text(host.HostName),
                new Text(host.User),
                new Text(host.Port.ToString()),
                new Text(authLabel));
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void WaitForKey()
    {
        AnsiConsole.MarkupLine("Press any key to continue...");
        System.Console.ReadKey(true);
    }
}