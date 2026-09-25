using Microsoft.Extensions.Logging;
using Spectre.Console;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public class MainMenuService : IMainMenuService
{
    private readonly IConfigRepository _configRepository;
    private readonly IServerCrudService _serverCrud;
    private readonly IGroupManagementService _groupManagement;
    private readonly IGroupMenuService _groupMenu;
    private readonly IServerMenuService _serverMenu;
    private readonly ISshRunnerService _sshRunner;
    private readonly ILogger<MainMenuService> _logger;

    public MainMenuService(
        IConfigRepository configRepository,
        IServerCrudService serverCrud,
        IGroupManagementService groupManagement,
        IGroupMenuService groupMenu,
        IServerMenuService serverMenu,
        ISshRunnerService sshRunner,
        ILogger<MainMenuService> logger)
    {
        _configRepository = configRepository;
        _serverCrud = serverCrud;
        _groupManagement = groupManagement;
        _groupMenu = groupMenu;
        _serverMenu = serverMenu;
        _sshRunner = sshRunner;
        _logger = logger;
    }

    public Task ShowAsync()
    {
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            _logger.LogWarning("Terminal is not interactive — skipping main menu");
            return Task.CompletedTask;
        }

        var running = true;

        while (running)
        {
            var config = _configRepository.Load();
            AnsiConsole.Clear();
            RenderServerTable(config);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Main Menu[/]")
                    .PageSize(10)
                    .AddChoices([
                        "[[C]] Connect",
                        "[[S]] Servers",
                        "[[G]] Groups",
                        "[[Q]] Quit"
                    ]));

            switch (choice)
            {
                case "[[C]] Connect":
                    ConnectToServer(config);
                    break;
                case "[[S]] Servers":
                    ShowServersMenu(config);
                    break;
                case "[[G]] Groups":
                    ShowGroupsMenu(config);
                    break;
                case "[[Q]] Quit":
                    running = false;
                    break;
            }
        }

        return Task.CompletedTask;
    }

    private void ShowServersMenu(AppConfig config)
    {
        var running = true;

        while (running)
        {
            config = _configRepository.Load();
            AnsiConsole.Clear();
            RenderServerTable(config);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Servers[/]")
                    .PageSize(10)
                    .AddChoices([
                        "[[S]] Show all",
                        "[[A]] Add",
                        "[[E]] Edit",
                        "[[D]] Delete",
                        "[[B]] Back"
                    ]));

            switch (choice)
            {
                case "[[S]] Show all":
                    _serverMenu.SelectServer();
                    break;
                case "[[A]] Add":
                    AddServer();
                    break;
                case "[[E]] Edit":
                    EditServer(config);
                    break;
                case "[[D]] Delete":
                    DeleteServer(config);
                    break;
                case "[[B]] Back":
                    running = false;
                    break;
            }
        }
    }

    private void ShowGroupsMenu(AppConfig config)
    {
        var running = true;

        while (running)
        {
            config = _configRepository.Load();
            AnsiConsole.Clear();
            RenderServerTable(config);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Groups[/]")
                    .PageSize(10)
                    .AddChoices([
                        "[[S]] Show groups",
                        "[[A]] Add",
                        "[[E]] Edit",
                        "[[D]] Delete",
                        "[[B]] Back"
                    ]));

            switch (choice)
            {
                case "[[S]] Show groups":
                    _groupMenu.Browse();
                    break;
                case "[[A]] Add":
                    CreateNewGroup(config);
                    break;
                case "[[E]] Edit":
                    RenameGroup(config);
                    break;
                case "[[D]] Delete":
                    DeleteGroup(config);
                    break;
                case "[[B]] Back":
                    running = false;
                    break;
            }
        }
    }

    private void ConnectToServer(AppConfig config)
    {
        if (config.Hosts.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No hosts to connect to.[/]");
            WaitForKey();
            return;
        }

        var hostLabels = config.Hosts
            .Select(h => (Host: h, Label: $"{Markup.Escape(h.Alias)} ({Markup.Escape(h.HostName)})"))
            .ToList();

        var selectedLabel = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [green]server[/] to connect to:")
                .PageSize(10)
                .AddChoices(hostLabels.Select(hl => hl.Label)));

        var host = hostLabels.First(hl => hl.Label == selectedLabel).Host;

        AnsiConsole.Console.Profile.Capabilities.Interactive = false;
        System.Console.ResetColor();

        _sshRunner.ConnectAsync(host).GetAwaiter().GetResult();

        System.Console.ResetColor();
        AnsiConsole.Console.Profile.Capabilities.Interactive = true;
    }

    private void EditServer(AppConfig config)
    {
        if (config.Hosts.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No hosts to edit.[/]");
            WaitForKey();
            return;
        }

        var hostLabels = config.Hosts
            .Select(h => (Host: h, Label: $"{Markup.Escape(h.Alias)} ({Markup.Escape(h.HostName)})"))
            .ToList();

        var selectedLabel = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [green]server[/] to edit:")
                .PageSize(10)
                .AddChoices(hostLabels.Select(hl => hl.Label)));

        var host = hostLabels.First(hl => hl.Label == selectedLabel).Host;

        _serverCrud.Edit(host);
        AnsiConsole.MarkupLine($"[green]✓[/] Server '[cyan]{Markup.Escape(host.Alias)}[/]' updated.");
        WaitForKey();
    }

    private void DeleteServer(AppConfig config)
    {
        if (config.Hosts.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No hosts to delete.[/]");
            WaitForKey();
            return;
        }

        var hostLabels = config.Hosts
            .Select(h => (Host: h, Label: $"{Markup.Escape(h.Alias)} ({Markup.Escape(h.HostName)})"))
            .ToList();

        var selectedLabel = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [green]server[/] to delete:")
                .PageSize(10)
                .AddChoices(hostLabels.Select(hl => hl.Label)));

        var host = hostLabels.First(hl => hl.Label == selectedLabel).Host;

        if (_serverCrud.Delete(host))
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Server '[cyan]{Markup.Escape(host.Alias)}[/]' deleted.");
        }
        else
        {
            AnsiConsole.MarkupLine("[grey]Deletion cancelled.[/]");
        }
        WaitForKey();
    }

    private void AddServer()
    {
        var host = _serverCrud.Add();
        if (host is not null)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Server '[cyan]{Markup.Escape(host.Alias)}[/]' added.");
        }
        else
        {
            AnsiConsole.MarkupLine("[grey]Add server cancelled.[/]");
        }
        WaitForKey();
    }

    private void CreateNewGroup(AppConfig config)
    {
        var name = AnsiConsole.Ask<string>("Enter [green]group name[/] (or leave empty to cancel):");

        if (string.IsNullOrWhiteSpace(name))
        {
            AnsiConsole.MarkupLine("[grey]Create group cancelled.[/]");
            WaitForKey();
            return;
        }

        _groupManagement.Create(name.Trim());
        AnsiConsole.MarkupLine($"[green]✓[/] Group '[cyan]{name.Trim()}[/]' created.");
        WaitForKey();
    }

    private void RenameGroup(AppConfig config)
    {
        var groups = _groupManagement.GetAll();
        if (groups.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No groups to rename.[/]");
            WaitForKey();
            return;
        }

        var groupNames = groups.Select(g => g.Name).ToList();
        var selectedName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [green]group[/] to rename:")
                .PageSize(10)
                .AddChoices(groupNames));

        var group = groups.First(g => g.Name == selectedName);
        var newName = AnsiConsole.Ask<string>($"Enter new name for [cyan]{Markup.Escape(group.Name)}[/] (or leave empty to cancel):");

        if (string.IsNullOrWhiteSpace(newName))
        {
            AnsiConsole.MarkupLine("[grey]Rename cancelled.[/]");
            WaitForKey();
            return;
        }

        _groupManagement.Rename(group.Id, newName.Trim());
        AnsiConsole.MarkupLine($"[green]✓[/] Group renamed to '[cyan]{Markup.Escape(newName.Trim())}[/]'.");
        WaitForKey();
    }

    private void DeleteGroup(AppConfig config)
    {
        var groups = _groupManagement.GetAll();
        if (groups.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No groups to delete.[/]");
            WaitForKey();
            return;
        }

        var groupNames = groups.Select(g => g.Name).ToList();
        var selectedName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [green]group[/] to delete:")
                .PageSize(10)
                .AddChoices(groupNames));

        var group = groups.First(g => g.Name == selectedName);

        if (AnsiConsole.Confirm($"Delete group '[cyan]{Markup.Escape(group.Name)}[/]'? Hosts in this group will become uncategorized."))
        {
            _groupManagement.Delete(group.Id);
            AnsiConsole.MarkupLine($"[green]✓[/] Group '[cyan]{Markup.Escape(group.Name)}[/]' deleted.");
        }
        else
        {
            AnsiConsole.MarkupLine("[grey]Deletion cancelled.[/]");
        }
        WaitForKey();
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