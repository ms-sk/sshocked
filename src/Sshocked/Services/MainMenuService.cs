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
        _logger = logger;
    }

    public Task ShowAsync()
    {
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            _logger.LogWarning("Terminal is not interactive -- skipping main menu");
            return Task.CompletedTask;
        }

        var running = true;

        while (running)
        {
            var config = _configRepository.Load();
            AnsiConsole.Clear();
            _tableRenderer.RenderServerTable(config);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Main Menu[/]")
                    .PageSize(10)
                    .AddChoices([
                        MenuLabels.Connect,
                        MenuLabels.Servers,
                        MenuLabels.Groups,
                        MenuLabels.Exit
                    ]));

            switch (choice)
            {
                case var c when c == MenuLabels.Connect:
                    ConnectToServer(config);
                    break;
                case var c when c == MenuLabels.Servers:
                    ShowServersMenu(config);
                    break;
                case var c when c == MenuLabels.Groups:
                    ShowGroupsMenu(config);
                    break;
                case var c when c == MenuLabels.Exit:
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
            _tableRenderer.RenderServerTable(config);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Servers[/]")
                    .PageSize(10)
                    .AddChoices([
                        MenuLabels.ShowAll,
                        MenuLabels.Add,
                        MenuLabels.Edit,
                        MenuLabels.Delete,
                        MenuLabels.Back
                    ]));

            switch (choice)
            {
                case var c when c == MenuLabels.ShowAll:
                    _serverMenu.SelectServer();
                    break;
                case var c when c == MenuLabels.Add:
                    AddServer();
                    break;
                case var c when c == MenuLabels.Edit:
                    EditServer(config);
                    break;
                case var c when c == MenuLabels.Delete:
                    DeleteServer(config);
                    break;
                case var c when c == MenuLabels.Back:
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
            _tableRenderer.RenderServerTable(config);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Groups[/]")
                    .PageSize(10)
                    .AddChoices([
                        MenuLabels.ShowGroups,
                        MenuLabels.Add,
                        MenuLabels.Edit,
                        MenuLabels.Delete,
                        MenuLabels.Back
                    ]));

            switch (choice)
            {
                case var c when c == MenuLabels.ShowGroups:
                    _groupMenu.Browse();
                    break;
                case var c when c == MenuLabels.Add:
                    CreateNewGroup(config);
                    break;
                case var c when c == MenuLabels.Edit:
                    RenameGroup(config);
                    break;
                case var c when c == MenuLabels.Delete:
                    DeleteGroup(config);
                    break;
                case var c when c == MenuLabels.Back:
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
            _consoleHelper.WaitForKey();
            return;
        }

        var host = _hostSelector.SelectHost(config.Hosts, "Select a [green]server[/] to connect to");
        if (host is null)
        {
            return;
        }

        // If host has Docker containers, offer to connect to a container instead
        var containers = host.DockerInfo?.Containers;
        if (containers is { Count: > 0 })
        {
            var containerLabels = containers
                .Select(c => (Container: c, Label: $"{Markup.Escape(c.Name)}  [grey]({c.Image})[/]"))
                .ToList();

            var choices = containerLabels.Select(cl => cl.Label).ToList();
            choices.Insert(0, MenuLabels.Connect);
            choices.Add(MenuLabels.Back);

            var selectedLabel = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[bold yellow]Connect to {Markup.Escape(host.Alias)}[/]")
                    .PageSize(10)
                    .AddChoices(choices));

            if (selectedLabel == MenuLabels.Connect)
            {
                _consoleHelper.RunSsh(host);
            }
            else if (selectedLabel != MenuLabels.Back)
            {
                var container = containerLabels.First(cl => cl.Label == selectedLabel).Container;
                ShowContainerActions(host, container);
            }

            return;
        }

        _consoleHelper.RunSsh(host);
    }

    private void ShowContainerActions(ServerHost host, ContainerModel container)
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold yellow]{Markup.Escape(container.Name)}[/]")
                .PageSize(10)
                .AddChoices([
                    "[[E]] Exec (sh)",
                    "[[L]] Logs (-f)",
                    MenuLabels.Back
                ]));

        switch (choice)
        {
            case "[[E]] Exec (sh)":
                _consoleHelper.RunDockerExec(host, container);
                break;
            case "[[L]] Logs (-f)":
                _consoleHelper.RunDockerLogs(host, container);
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
