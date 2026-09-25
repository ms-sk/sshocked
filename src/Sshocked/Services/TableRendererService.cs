using Spectre.Console;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class TableRendererService : ITableRendererService
{
    public void RenderServerTable(AppConfig config)
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

    public void RenderGroup(string groupName, List<ServerHost> hosts)
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
            var authLabel = GetAuthLabel(host.AuthType);

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

    public string GetAuthLabel(AuthType authType) => authType switch
    {
        AuthType.Password => "Password",
        AuthType.SshAgent => "Agent",
        AuthType.CustomConfig => "Custom",
        _ => "SSH Key"
    };
}
