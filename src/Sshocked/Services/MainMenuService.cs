using Microsoft.Extensions.Logging;
using Spectre.Console;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class MainMenuService(IConfigRepository configRepository, IServerCrudService serverCrud, IGroupManagementService groupManagement, IGroupMenuService groupMenu, IServerMenuService serverMenu, ITableRendererService tableRenderer, IHostSelectorService hostSelector, IConsoleHelperService consoleHelper, IGroupConnectionService groupConnection, IProcessService processService, IMenuFactory menuFactory, ILogger<MainMenuService> logger) : IMainMenuService
{

    public async Task Show()
    {
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            logger.LogWarning("Terminal is not interactive -- skipping main menu");
            return;
        }

        var running = true;

        while (running)
        {
            var config = configRepository.Load();
            AnsiConsole.Clear();
            tableRenderer.RenderServerTable(config);

            var menuEntries = BuildMainMenuEntries(config);
            running = await menuFactory.RunMenu("[bold yellow]Main Menu[/]", menuEntries);
        }
    }

    private List<MenuEntry> BuildMainMenuEntries(AppConfig config)
    {
        return new List<MenuEntry>
        {
            new("Connect", 'C', executeAsync: () => ConnectToServer(config)),
            new("Connect Group", 'G', executeAsync: () => ConnectToGroup(config)),
            new("Run command", 'X', executeAsync: () => RunCommandOnGroup(config)),
            new("Servers", 'S', executeAsync: () => ShowServersMenu(config)),
            new("Groups", 'R', executeAsync: () => ShowGroupsMenu(config)),
            new("Exit", 'E', actionType: MenuActionType.Exit)
        };
    }

    private async Task ShowServersMenu(AppConfig config)
    {
        var running = true;

        while (running)
        {
            config = configRepository.Load();
            AnsiConsole.Clear();
            tableRenderer.RenderServerTable(config);

            var menuEntries = new List<MenuEntry>
            {
                new("Show all", 'S', executeAsync: () => serverMenu.SelectServer()),
                new("Add", 'A', executeAsync: () => { AddServer(); return Task.CompletedTask; }),
                new("Edit", 'E', executeAsync: () => { EditServer(config); return Task.CompletedTask; }),
                new("Delete", 'D', executeAsync: () => { DeleteServer(config); return Task.CompletedTask; }),
                new("Back", 'B', actionType: MenuActionType.Back)
            };

            running = await menuFactory.RunMenu("[bold yellow]Servers[/]", menuEntries);
        }
    }

    private async Task ShowGroupsMenu(AppConfig config)
    {
        var running = true;

        while (running)
        {
            config = configRepository.Load();
            AnsiConsole.Clear();
            tableRenderer.RenderServerTable(config);

            var menuEntries = new List<MenuEntry>
            {
                new("Show groups", 'S', executeAsync: () => groupMenu.Browse()),
                new("Add", 'A', executeAsync: () => { CreateNewGroup(config); return Task.CompletedTask; }),
                new("Edit", 'E', executeAsync: () => { RenameGroup(config); return Task.CompletedTask; }),
                new("Delete", 'D', executeAsync: () => { DeleteGroup(config); return Task.CompletedTask; }),
                new("Back", 'B', actionType: MenuActionType.Back)
            };

            running = await menuFactory.RunMenu("[bold yellow]Groups[/]", menuEntries);
        }
    }

    private async Task ConnectToGroup(AppConfig config)
    {
        var groups = groupManagement.GetAll();
        if (groups.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No groups configured. Create one first.[/]");
            consoleHelper.WaitForKey();
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
            consoleHelper.WaitForKey();
            return;
        }

        var menuEntries = new List<MenuEntry>
            {
                new("Sequential (one after another)", 'S', executeAsync: () => groupConnection.ConnectAllSequential(hosts)),
                new("Multi-Tab (new terminal windows)", 'M', executeAsync: () => groupConnection.ConnectAllMultiTab(hosts)),
                new("Back", 'B', actionType: MenuActionType.Back)
            };

        await menuFactory.RunMenu($"[bold yellow]Connect group: {Markup.Escape(group.Name)}[/]", menuEntries);
    }

    private async Task RunCommandOnGroup(AppConfig config)
    {
        var groups = groupManagement.GetAll();
        if (groups.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No groups configured. Create one first.[/]");
            consoleHelper.WaitForKey();
            return;
        }

        var groupNames = groups.Select(g => g.Name).ToList();
        groupNames.Add(MenuLabels.Back);

        var selectedName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [green]group[/] ([grey]clear search for Back[/]):")
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
            consoleHelper.WaitForKey();
            return;
        }

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold cyan]Group: {Markup.Escape(group.Name)}[/]");
        AnsiConsole.WriteLine();
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
                    var output = await processService.RunSshCommand(host, trimmed);
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

    private async Task ConnectToServer(AppConfig config)
    {
        if (config.Hosts.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No hosts to connect to.[/]");
            consoleHelper.WaitForKey();
            return;
        }

        var host = hostSelector.SelectHost(config.Hosts, "Select a [green]server[/] to connect to");
        if (host is null)
        {
            return;
        }

        var containers = host.DockerInfo?.Containers;
        if (containers is { Count: > 0 })
        {
            var menuEntries = new List<MenuEntry>
            {
                new("Connect", 'C', executeAsync: () => consoleHelper.RunSsh(host))
            };

            foreach (var container in containers)
            {
                var capturedContainer = container;
                menuEntries.Add(new MenuEntry(
                    $"{Markup.Escape(container.Name)}  [grey]({container.Image})[/]",
                    executeAsync: () => ShowContainerActions(host, capturedContainer)));
            }

            menuEntries.Add(new MenuEntry("Back", 'B', actionType: MenuActionType.Back));

            await menuFactory.RunMenu($"[bold yellow]Connect to {Markup.Escape(host.Alias)}[/]", menuEntries);
            return;
        }

        await consoleHelper.RunSsh(host);
    }

    private async Task ShowContainerActions(ServerHost host, ContainerModel container)
    {
        var menuEntries = new List<MenuEntry>
        {
            new("Exec (sh)", 'E', executeAsync: () => consoleHelper.RunDockerExec(host, container)),
            new("Logs (-f)", 'L', executeAsync: () => consoleHelper.RunDockerLogs(host, container)),
            new("Back", 'B', actionType: MenuActionType.Back)
        };

        await menuFactory.RunMenu($"[bold yellow]{Markup.Escape(container.Name)}[/]", menuEntries);
    }

    private void EditServer(AppConfig config)
    {
        if (config.Hosts.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No hosts to edit.[/]");
            consoleHelper.WaitForKey();
            return;
        }

        var host = hostSelector.SelectHost(config.Hosts, "Select a [green]server[/] to edit");
        if (host is null)
        {
            return;
        }

        serverCrud.Edit(host);
        AnsiConsole.MarkupLine($"[green]\u2713[/] Server '[cyan]{Markup.Escape(host.Alias)}[/]' updated.");
        consoleHelper.WaitForKey();
    }

    private void DeleteServer(AppConfig config)
    {
        if (config.Hosts.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No hosts to delete.[/]");
            consoleHelper.WaitForKey();
            return;
        }

        var host = hostSelector.SelectHost(config.Hosts, "Select a [green]server[/] to delete");
        if (host is null)
        {
            return;
        }

        if (serverCrud.Delete(host))
        {
            AnsiConsole.MarkupLine($"[green]\u2713[/] Server '[cyan]{Markup.Escape(host.Alias)}[/]' deleted.");
        }
        else
        {
            AnsiConsole.MarkupLine("[grey]Deletion cancelled.[/]");
        }
        consoleHelper.WaitForKey();
    }

    private void AddServer()
    {
        var host = serverCrud.Add();
        if (host is not null)
        {
            AnsiConsole.MarkupLine($"[green]\u2713[/] Server '[cyan]{Markup.Escape(host.Alias)}[/]' added.");
        }
        else
        {
            AnsiConsole.MarkupLine("[grey]Add server cancelled.[/]");
        }
        consoleHelper.WaitForKey();
    }

    private void CreateNewGroup(AppConfig config)
    {
        var name = AnsiConsole.Ask<string>("Enter [green]group name[/] (or leave empty to cancel):");

        if (string.IsNullOrWhiteSpace(name))
        {
            AnsiConsole.MarkupLine("[grey]Create group cancelled.[/]");
            consoleHelper.WaitForKey();
            return;
        }

        groupManagement.Create(name.Trim());
        AnsiConsole.MarkupLine($"[green]\u2713[/] Group '[cyan]{name.Trim()}[/]' created.");
        consoleHelper.WaitForKey();
    }

    private void RenameGroup(AppConfig config)
    {
        var groups = groupManagement.GetAll();
        if (groups.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No groups to rename.[/]");
            consoleHelper.WaitForKey();
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
            consoleHelper.WaitForKey();
            return;
        }

        groupManagement.Rename(group.Id, newName.Trim());
        AnsiConsole.MarkupLine($"[green]\u2713[/] Group renamed to '[cyan]{Markup.Escape(newName.Trim())}[/]'.");
        consoleHelper.WaitForKey();
    }

    private void DeleteGroup(AppConfig config)
    {
        var groups = groupManagement.GetAll();
        if (groups.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No groups to delete.[/]");
            consoleHelper.WaitForKey();
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
            groupManagement.Delete(group.Id);
            AnsiConsole.MarkupLine($"[green]\u2713[/] Group '[cyan]{Markup.Escape(group.Name)}[/]' deleted.");
        }
        else
        {
            AnsiConsole.MarkupLine("[grey]Deletion cancelled.[/]");
        }
        consoleHelper.WaitForKey();
    }
}


