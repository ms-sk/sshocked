using Microsoft.Extensions.Logging;
using Spectre.Console;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class MainMenuService : IMainMenuService
{
    private readonly IConfigRepository _configRepository;
    private readonly IServerCrudService _serverCrud;
    private readonly IGroupManagementService _groupManagement;
    private readonly IGroupMenuService _groupMenu;
    private readonly IServerMenuService _serverMenu;
    private readonly ITableRendererService _tableRenderer;
    private readonly IHostSelectorService _hostSelector;
    private readonly IConsoleHelperService _consoleHelper;
    private readonly IKeyboardShortcutService _keyboardShortcut;
    private readonly IGroupConnectionService _groupConnection;
    private readonly ILogger<MainMenuService> _logger;

    public MainMenuService(
        IConfigRepository configRepository,
        IServerCrudService serverCrud,
        IGroupManagementService groupManagement,
        IGroupMenuService groupMenu,
        IServerMenuService serverMenu,
        ITableRendererService tableRenderer,
        IHostSelectorService hostSelector,
        IConsoleHelperService consoleHelper,
        IKeyboardShortcutService keyboardShortcut,
        IGroupConnectionService groupConnection,
        ILogger<MainMenuService> logger)
    {
        _configRepository = configRepository;
        _serverCrud = serverCrud;
        _groupManagement = groupManagement;
        _groupMenu = groupMenu;
        _serverMenu = serverMenu;
        _tableRenderer = tableRenderer;
        _hostSelector = hostSelector;
        _consoleHelper = consoleHelper;
        _keyboardShortcut = keyboardShortcut;
        _groupConnection = groupConnection;
        _logger = logger;
    }

    public async Task Show()
    {
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            _logger.LogWarning("Terminal is not interactive -- skipping main menu");
            return;
        }

        var running = true;

        while (running)
        {
            var config = _configRepository.Load();
            AnsiConsole.Clear();
            _tableRenderer.RenderServerTable(config);

            var choice = _keyboardShortcut.ShowMenu(
                "[bold yellow]Main Menu[/]",
                [
                    new MenuEntry("Connect", 'C'),
                    new MenuEntry("Connect Group", 'G'),
                    new MenuEntry("Servers", 'S'),
                    new MenuEntry("Groups", 'R'),
                    new MenuEntry("Exit", 'E')
                ]);

            switch (choice?.Label)
            {
                case "Connect":
                    await ConnectToServer(config);
                    break;
                case "Connect Group":
                    await ConnectToGroup(config);
                    break;
                case "Servers":
                    await ShowServersMenu(config);
                    break;
                case "Groups":
                    await ShowGroupsMenu(config);
                    break;
                case "Exit":
                    running = false;
                    break;
            }
        }
    }

    private async Task ShowServersMenu(AppConfig config)
    {
        var running = true;

        while (running)
        {
            config = _configRepository.Load();
            AnsiConsole.Clear();
            _tableRenderer.RenderServerTable(config);

            var choice = _keyboardShortcut.ShowMenu(
                "[bold yellow]Servers[/]",
                [
                    new MenuEntry("Show all", 'S'),
                    new MenuEntry("Add", 'A'),
                    new MenuEntry("Edit", 'E'),
                    new MenuEntry("Delete", 'D'),
                    new MenuEntry("Back", 'B')
                ]);

            switch (choice?.Label)
            {
                case "Show all":
                    await _serverMenu.SelectServer();
                    break;
                case "Add":
                    AddServer();
                    break;
                case "Edit":
                    EditServer(config);
                    break;
                case "Delete":
                    DeleteServer(config);
                    break;
                case "Back":
                    running = false;
                    break;
            }
        }
    }

    private async Task ShowGroupsMenu(AppConfig config)
    {
        var running = true;

        while (running)
        {
            config = _configRepository.Load();
            AnsiConsole.Clear();
            _tableRenderer.RenderServerTable(config);

            var choice = _keyboardShortcut.ShowMenu(
                "[bold yellow]Groups[/]",
                [
                    new MenuEntry("Show groups", 'S'),
                    new MenuEntry("Add", 'A'),
                    new MenuEntry("Edit", 'E'),
                    new MenuEntry("Delete", 'D'),
                    new MenuEntry("Back", 'B')
                ]);

            switch (choice?.Label)
            {
                case "Show groups":
                    await _groupMenu.Browse();
                    break;
                case "Add":
                    CreateNewGroup(config);
                    break;
                case "Edit":
                    RenameGroup(config);
                    break;
                case "Delete":
                    DeleteGroup(config);
                    break;
                case "Back":
                    running = false;
                    break;
            }
        }
    }

    private async Task ConnectToGroup(AppConfig config)
    {
        var groups = _groupManagement.GetAll();
        if (groups.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No groups configured. Create one first.[/]");
            _consoleHelper.WaitForKey();
            return;
        }

        var groupNames = groups.Select(g => g.Name).ToList();
        groupNames.Add(MenuLabels.Back);

        var selectedName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [green]group[/] to connect ([grey]clear search for Back[/]):")
                .PageSize(10)
                .EnableSearch()
                .AddChoices(groupNames));

        if (selectedName == MenuLabels.Back)
        {
            return;
        }

        var group = groups.First(g => g.Name == selectedName);
        var hosts = config.Hosts.Where(h => h.GroupId == group.Id).ToList();

        if (hosts.Count == 0)
        {
            AnsiConsole.MarkupLine($"[red]No servers in group '[cyan]{Markup.Escape(group.Name)}[/]'.[/]");
            _consoleHelper.WaitForKey();
            return;
        }

        if (_groupConnection.CanLaunchMultiTab)
        {
            var choice = _keyboardShortcut.ShowMenu(
                $"[bold yellow]Connect group: {Markup.Escape(group.Name)}[/]",
                [
                    new MenuEntry("Sequential (one after another)", 'S', ConnectionStrategy.Sequential),
                    new MenuEntry("Multi-Tab (new terminal windows)", 'M', ConnectionStrategy.MultiTab),
                    new MenuEntry("Back", 'B')
                ]);

            switch (choice?.Tag)
            {
                case ConnectionStrategy.Sequential:
                    await _groupConnection.ConnectAllSequential(hosts);
                    break;
                case ConnectionStrategy.MultiTab:
                    await _groupConnection.ConnectAllMultiTab(hosts);
                    break;
            }

            return;
        }

        await _groupConnection.ConnectAllSequential(hosts);
    }

    private async Task ConnectToServer(AppConfig config)
    {
        if (config.Hosts.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No hosts to connect to.[/]");
            _consoleHelper.WaitForKey();
            return;
        }

        var host = _hostSelector.SelectHost(config.Hosts, "Select a [green]server[/] to connect to");
        if (host is null)
        {
            return;
        }

        var containers = host.DockerInfo?.Containers;
        if (containers is { Count: > 0 })
        {
            var containerEntries = containers
                .Select(c => (Container: c, Entry: new MenuEntry($"{Markup.Escape(c.Name)}  [grey]({c.Image})[/]")))
                .ToList();

            var choices = new List<MenuEntry> { new("Connect", 'C') };
            choices.AddRange(containerEntries.Select(ce => ce.Entry));
            choices.Add(new MenuEntry("Back", 'B'));

            var selected = _keyboardShortcut.ShowMenu(
                $"[bold yellow]Connect to {Markup.Escape(host.Alias)}[/]",
                choices);

            if (selected?.Label == "Connect")
            {
                await _consoleHelper.RunSsh(host);
            }
            else if (selected?.Label != "Back" && selected is not null)
            {
                var container = containerEntries.First(ce => ce.Entry.Label == selected.Label).Container;
                await ShowContainerActions(host, container);
            }

            return;
        }

        await _consoleHelper.RunSsh(host);
    }

    private async Task ShowContainerActions(ServerHost host, ContainerModel container)
    {
        var choice = _keyboardShortcut.ShowMenu(
            $"[bold yellow]{Markup.Escape(container.Name)}[/]",
            [
                new MenuEntry("Exec (sh)", 'E'),
                new MenuEntry("Logs (-f)", 'L'),
                new MenuEntry("Back", 'B')
            ]);

        switch (choice?.Label)
        {
            case "Exec (sh)":
                await _consoleHelper.RunDockerExec(host, container);
                break;
            case "Logs (-f)":
                await _consoleHelper.RunDockerLogs(host, container);
                break;
        }
    }

    private void EditServer(AppConfig config)
    {
        if (config.Hosts.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No hosts to edit.[/]");
            _consoleHelper.WaitForKey();
            return;
        }

        var host = _hostSelector.SelectHost(config.Hosts, "Select a [green]server[/] to edit");
        if (host is null)
        {
            return;
        }

        _serverCrud.Edit(host);
        AnsiConsole.MarkupLine($"[green]\u2713[/] Server '[cyan]{Markup.Escape(host.Alias)}[/]' updated.");
        _consoleHelper.WaitForKey();
    }

    private void DeleteServer(AppConfig config)
    {
        if (config.Hosts.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No hosts to delete.[/]");
            _consoleHelper.WaitForKey();
            return;
        }

        var host = _hostSelector.SelectHost(config.Hosts, "Select a [green]server[/] to delete");
        if (host is null)
        {
            return;
        }

        if (_serverCrud.Delete(host))
        {
            AnsiConsole.MarkupLine($"[green]\u2713[/] Server '[cyan]{Markup.Escape(host.Alias)}[/]' deleted.");
        }
        else
        {
            AnsiConsole.MarkupLine("[grey]Deletion cancelled.[/]");
        }
        _consoleHelper.WaitForKey();
    }

    private void AddServer()
    {
        var host = _serverCrud.Add();
        if (host is not null)
        {
            AnsiConsole.MarkupLine($"[green]\u2713[/] Server '[cyan]{Markup.Escape(host.Alias)}[/]' added.");
        }
        else
        {
            AnsiConsole.MarkupLine("[grey]Add server cancelled.[/]");
        }
        _consoleHelper.WaitForKey();
    }

    private void CreateNewGroup(AppConfig config)
    {
        var name = AnsiConsole.Ask<string>("Enter [green]group name[/] (or leave empty to cancel):");

        if (string.IsNullOrWhiteSpace(name))
        {
            AnsiConsole.MarkupLine("[grey]Create group cancelled.[/]");
            _consoleHelper.WaitForKey();
            return;
        }

        _groupManagement.Create(name.Trim());
        AnsiConsole.MarkupLine($"[green]\u2713[/] Group '[cyan]{name.Trim()}[/]' created.");
        _consoleHelper.WaitForKey();
    }

    private void RenameGroup(AppConfig config)
    {
        var groups = _groupManagement.GetAll();
        if (groups.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No groups to rename.[/]");
            _consoleHelper.WaitForKey();
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
            _consoleHelper.WaitForKey();
            return;
        }

        _groupManagement.Rename(group.Id, newName.Trim());
        AnsiConsole.MarkupLine($"[green]\u2713[/] Group renamed to '[cyan]{Markup.Escape(newName.Trim())}[/]'.");
        _consoleHelper.WaitForKey();
    }

    private void DeleteGroup(AppConfig config)
    {
        var groups = _groupManagement.GetAll();
        if (groups.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No groups to delete.[/]");
            _consoleHelper.WaitForKey();
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
            AnsiConsole.MarkupLine($"[green]\u2713[/] Group '[cyan]{Markup.Escape(group.Name)}[/]' deleted.");
        }
        else
        {
            AnsiConsole.MarkupLine("[grey]Deletion cancelled.[/]");
        }
        _consoleHelper.WaitForKey();
    }
}
