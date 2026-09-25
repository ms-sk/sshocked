using Microsoft.Extensions.Logging;
using Spectre.Console;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public class ServerMenuService : IServerMenuService
{
    private readonly IConfigRepository _configRepository;
    private readonly IServerCrudService _serverCrud;
    private readonly IProcessService _processService;
    private readonly INavigationService _nav;
    private readonly ILogger<ServerMenuService> _logger;

    public ServerMenuService(
        IConfigRepository configRepository,
        IServerCrudService serverCrud,
        IProcessService processService,
        INavigationService nav,
        ILogger<ServerMenuService> logger)
    {
        _configRepository = configRepository;
        _serverCrud = serverCrud;
        _processService = processService;
        _nav = nav;
        _logger = logger;
    }

    public void SelectServer()
    {
        var config = _configRepository.Load();
        if (config.Hosts.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No servers configured.[/]");
            WaitForKey();
            return;
        }

        _nav.Push(ViewType.ServerDetail);

        while (_nav.Current == ViewType.ServerDetail)
        {
            config = _configRepository.Load();
            AnsiConsole.Clear();
            RenderServerTable(config);

            var hostLabels = config.Hosts
                .Select(h => (Host: h, Label: $"{Markup.Escape(h.Alias)} ({Markup.Escape(h.HostName)})"))
                .ToList();

            var selectedLabel = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a [green]server[/] ([grey]Esc[/] to go back):")
                    .PageSize(10)
                    .EnableSearch()
                    .AddChoices(hostLabels.Select(hl => hl.Label)));

            var host = hostLabels.First(hl => hl.Label == selectedLabel).Host;
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
                        "[[C]] Connect",
                        "[[E]] Edit",
                        "[[D]] Delete",
                        "[[B]] Back"
                    ]));

            switch (choice)
            {
                case "[[C]] Connect":
                    RunSsh(host);
                    break;
                case "[[E]] Edit":
                    _serverCrud.Edit(host);
                    AnsiConsole.MarkupLine($"[green]✓[/] Server '[cyan]{Markup.Escape(host.Alias)}[/]' updated.");
                    WaitForKey();
                    break;
                case "[[D]] Delete":
                    if (_serverCrud.Delete(host))
                    {
                        AnsiConsole.MarkupLine($"[green]✓[/] Server '[cyan]{Markup.Escape(host.Alias)}[/]' deleted.");
                        WaitForKey();
                        _nav.Pop();
                        return;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[grey]Deletion cancelled.[/]");
                        WaitForKey();
                    }
                    break;
                case "[[B]] Back":
                    _nav.Pop();
                    return;
            }
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

    private static void RenderServerTable(AppConfig config)
    {
        var groups = config.Groups;
        var hosts = config.Hosts;

        if (hosts.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No hosts configured yet.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        var uncategorized = hosts.Where(h =>
            string.IsNullOrEmpty(h.GroupId) ||
            !groups.Any(g => g.Id == h.GroupId)).ToList();

        if (uncategorized.Count > 0)
        {
            RenderGroup("Uncategorized", uncategorized);
        }

        foreach (var group in groups)
        {
            var groupHosts = hosts.Where(h => h.GroupId == group.Id).ToList();
            if (groupHosts.Count > 0)
            {
                RenderGroup(group.Name, groupHosts);
            }
        }

        AnsiConsole.WriteLine();
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