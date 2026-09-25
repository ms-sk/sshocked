using Spectre.Console;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class HostSelectorService : IHostSelectorService
{
    public ServerHost? SelectHost(List<ServerHost> hosts, string title)
    {
        if (hosts.Count == 0)
        {
            return null;
        }

        var hostLabels = hosts
            .Select(h => (Host: h, Label: $"{Markup.Escape(h.Alias)} ({Markup.Escape(h.HostName)})"))
            .ToList();

        var backLabel = MenuLabels.Back;
        var choices = hostLabels.Select(hl => hl.Label).ToList();
        choices.Add(backLabel);

        var selectedLabel = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"{title} ([grey]clear search for Back[/]):")
                .PageSize(10)
                .EnableSearch()
                .AddChoices(choices));

        if (selectedLabel == backLabel)
        {
            return null;
        }

        return hostLabels.First(hl => hl.Label == selectedLabel).Host;
    }
}
