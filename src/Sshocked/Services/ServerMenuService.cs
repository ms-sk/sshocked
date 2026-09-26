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
    private readonly IDockerService _dockerService;
    private readonly INavigationService _nav;
    private readonly ILogger<ServerMenuService> _logger;

    public ServerMenuService(
        IConfigRepository configRepository,
        IServerCrudService serverCrud,
        ITableRendererService tableRenderer,
        IHostSelectorService hostSelector,
        IConsoleHelperService consoleHelper,
        IDockerService dockerService,
        INavigationService nav,
        ILogger<ServerMenuService> logger)
    {
        _configRepository = configRepository;
        _serverCrud = serverCrud;
        _tableRenderer = tableRenderer;
        _hostSelector = hostSelector;
        _consoleHelper = consoleHelper;
        _dockerService = dockerService;
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
            RenderServerHeader(host);
            RenderDockerSummary(host);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Server Actions[/]")
                    .PageSize(10)
                    .AddChoices([
                        MenuLabels.Connect,
                        MenuLabels.ScanDocker,
                        MenuLabels.Edit,
                        MenuLabels.Delete,
                        MenuLabels.Back
                    ]));

            switch (choice)
            {
                case var c when c == MenuLabels.Connect:
                    _consoleHelper.RunSsh(host);
                    break;
                case var c when c == MenuLabels.ScanDocker:
                    ScanDockerForHost(host);
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

    private void RenderServerHeader(ServerHost host)
    {
        AnsiConsole.MarkupLine($"[bold cyan]Server: {Markup.Escape(host.Alias)}[/]");
        AnsiConsole.MarkupLine($"  Host: {Markup.Escape(host.HostName)}  User: {Markup.Escape(host.User)}  Port: {host.Port}");
        AnsiConsole.WriteLine();
    }

    private void RenderDockerSummary(ServerHost host)
    {
        var dockerInfo = host.DockerInfo;
        if (dockerInfo is null)
        {
            AnsiConsole.MarkupLine("[grey]Docker: Not scanned yet. Use 'Scan Docker' to discover containers.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        if (!dockerInfo.DockerAvailable)
        {
            AnsiConsole.MarkupLine("[yellow]Docker: Not available on this host.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        var lastScan = dockerInfo.LastScannedAt.HasValue
            ? dockerInfo.LastScannedAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
            : "unknown";

        AnsiConsole.MarkupLine($"[green]Docker:[/] v{dockerInfo.ServerVersion ?? "?"}  {dockerInfo.OsType ?? "?"}  ({dockerInfo.Architecture ?? "?"})");
        AnsiConsole.MarkupLine($"[green]Containers:[/] {dockerInfo.ContainersRunning ?? 0} running / {dockerInfo.ContainersTotal ?? 0} total");
        AnsiConsole.MarkupLine($"[green]Compose Stacks:[/] {dockerInfo.ComposeStacks.Count}");
        AnsiConsole.MarkupLine($"[grey]Last scanned: {lastScan}[/]");
        AnsiConsole.WriteLine();

        // Show container table if there are containers
        if (dockerInfo.Containers.Count > 0)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[cyan]Containers[/]")
                .AddColumns([
                    new TableColumn("ID").Centered(),
                    new TableColumn("Name").Centered(),
                    new TableColumn("Image").Centered(),
                    new TableColumn("Status").Centered(),
                    new TableColumn("Ports").Centered()
                ]);

            foreach (var c in dockerInfo.Containers)
            {
                var shortId = c.ContainerId.Length >= 12 ? c.ContainerId[..12] : c.ContainerId;
                table.AddRow(
                    new Text(shortId),
                    new Markup($"[bold]{Markup.Escape(c.Name)}[/]"),
                    new Text(c.Image),
                    new Text(c.Status),
                    new Text(c.Ports ?? "-"));
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        // Show compose stacks table if there are stacks
        if (dockerInfo.ComposeStacks.Count > 0)
        {
            var stackTable = new Table()
                .Border(TableBorder.Rounded)
                .Title("[cyan]Compose Stacks[/]")
                .AddColumns([
                    new TableColumn("Name").Centered(),
                    new TableColumn("Status").Centered(),
                    new TableColumn("Config Files").Centered()
                ]);

            foreach (var s in dockerInfo.ComposeStacks)
            {
                stackTable.AddRow(
                    new Markup($"[bold]{Markup.Escape(s.Name)}[/]"),
                    new Text(s.Status),
                    new Text(s.ConfigFiles ?? "-"));
            }

            AnsiConsole.Write(stackTable);
            AnsiConsole.WriteLine();
        }
    }

    private void ScanDockerForHost(ServerHost host)
    {
        // Prompt for sudo password if not already set for this session
        if (string.IsNullOrEmpty(host.SudoPassword))
        {
            var needsSudo = AnsiConsole.Confirm("Does this host require [yellow]sudo[/] with a password?", false);
            if (needsSudo)
            {
                host.SudoPassword = AnsiConsole.Prompt(
                    new TextPrompt<string>("Enter [yellow]sudo password[/]:")
                        .Secret());
            }
        }

        AnsiConsole.MarkupLine("[yellow]Scanning host for Docker containers...[/]");
        AnsiConsole.MarkupLine("[grey]This may take a few seconds.[/]");

        // Run the scan synchronously in the TUI context
        var info = _dockerService.Scan(host).GetAwaiter().GetResult();

        if (!info.DockerAvailable)
        {
            AnsiConsole.MarkupLine("[yellow]Docker is not available on this host.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]\u2713[/] Scan complete: {info.Containers.Count} container(s), {info.ComposeStacks.Count} compose stack(s) found.");
        }

        _consoleHelper.WaitForKey();
    }
}
