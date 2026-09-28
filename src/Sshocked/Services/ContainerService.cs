using Microsoft.Extensions.Logging;
using Spectre.Console;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class ContainerService(
    IConfigRepository configRepository,
    IGroupManagementService groupManagement,
    IConsoleHelperService consoleHelper,
    ILogger<ContainerService> logger) : IContainerService
{
    public ContainerModel? Save(ServerHost serverHost, ContainerModel scannedContainer)
    {
        var config = configRepository.Load();
        var host = config.Hosts.FirstOrDefault(h => h.Id == serverHost.Id);

        if (host is null)
        {
            logger.LogWarning("Server host not found: {ServerHostId}", serverHost.Id);
            return null;
        }

        var existing = host.SavedContainers.FirstOrDefault(
            c => c.ContainerId == scannedContainer.ContainerId || c.Name == scannedContainer.Name);

        if (existing is not null)
        {
            AnsiConsole.MarkupLine($"[yellow]Container '[cyan]{Markup.Escape(scannedContainer.Name)}[/]' is already saved.[/]");
            consoleHelper.WaitForKey();
            return null;
        }

        var defaultAlias = $"{serverHost.Alias}/{scannedContainer.Name}";

        var alias = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Alias[/] (or [grey]leave empty to cancel[/]):")
                .DefaultValue(defaultAlias)
                .AllowEmpty()
                .Validate(input =>
                {
                    if (string.IsNullOrWhiteSpace(input))
                        return ValidationResult.Success();
                    if (config.Hosts.Any(h => h.SavedContainers.Any(sc => sc.Alias?.Equals(input.Trim(), StringComparison.OrdinalIgnoreCase) == true)))
                        return ValidationResult.Error("[red]Alias already exists.[/]");
                    return ValidationResult.Success();
                }));

        if (string.IsNullOrWhiteSpace(alias))
        {
            logger.LogInformation("User cancelled save container");
            return null;
        }

        var groups = groupManagement.GetAll();
        var groupName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]Group[/]:")
                .AddChoices(groups.Select(g => g.Name).Prepend("(none)")));

        var tagsInput = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Tags[/] (comma-separated):")
                .AllowEmpty());

        var savedContainer = new ContainerModel
        {
            Id = Guid.NewGuid().ToString("N"),
            Alias = alias.Trim(),
            ContainerId = scannedContainer.ContainerId,
            Name = scannedContainer.Name,
            Image = scannedContainer.Image,
            Status = scannedContainer.Status,
            Ports = scannedContainer.Ports,
            ComposeProject = scannedContainer.ComposeProject,
            GroupId = groupName == "(none)" ? string.Empty : groups.First(g => g.Name == groupName).Id,
            Tags = string.IsNullOrWhiteSpace(tagsInput)
                ? []
                : tagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
        };

        host.SavedContainers.Add(savedContainer);
        configRepository.Save(config);
        logger.LogInformation("Saved container '{Alias}' on server {ServerAlias}", savedContainer.Alias, serverHost.Alias);

        AnsiConsole.MarkupLine($"[green]\u2713[/] Container '[cyan]{Markup.Escape(savedContainer.Alias)}[/]' saved.");
        consoleHelper.WaitForKey();

        return savedContainer;
    }

    public void Edit(ServerHost serverHost, ContainerModel savedContainer)
    {
        var config = configRepository.Load();
        var host = config.Hosts.FirstOrDefault(h => h.Id == serverHost.Id);

        if (host is null) return;

        var existing = host.SavedContainers.FirstOrDefault(c => c.Id == savedContainer.Id);
        if (existing is null || existing.Alias is null) return;

        var alias = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Alias[/]:")
                .DefaultValue(existing.Alias)
                .Validate(input =>
                {
                    if (string.IsNullOrWhiteSpace(input))
                        return ValidationResult.Error("[red]Alias cannot be empty.[/]");
                    if (config.Hosts.Any(h => h.SavedContainers.Any(sc => sc.Id != existing.Id && sc.Alias?.Equals(input.Trim(), StringComparison.OrdinalIgnoreCase) == true)))
                        return ValidationResult.Error("[red]Alias already exists.[/]");
                    return ValidationResult.Success();
                }));

        var groups = groupManagement.GetAll();
        var currentGroup = groups.FirstOrDefault(g => g.Id == existing.GroupId);
        var groupName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]Group[/]:")
                .AddChoices(groups.Select(g => g.Name).Prepend("(none)"))
                .DefaultValue(currentGroup?.Name ?? "(none)"));

        var tagsInput = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Tags[/] (comma-separated):")
                .DefaultValue(string.Join(", ", existing.Tags)));

        existing.Alias = alias.Trim();
        existing.GroupId = groupName == "(none)" ? string.Empty : groups.First(g => g.Name == groupName).Id;
        existing.Tags = string.IsNullOrWhiteSpace(tagsInput)
            ? []
            : tagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        configRepository.Save(config);
        logger.LogInformation("Edited container '{Alias}' on server {ServerAlias}", existing.Alias, serverHost.Alias);
    }

    public bool Delete(ServerHost serverHost, ContainerModel savedContainer)
    {
        var alias = savedContainer.Alias ?? savedContainer.Name;
        var confirmed = AnsiConsole.Confirm(
            $"Are you sure you want to delete '[yellow]{Markup.Escape(alias)}[/]'?");

        if (!confirmed) return false;

        var config = configRepository.Load();
        var host = config.Hosts.FirstOrDefault(h => h.Id == serverHost.Id);

        if (host is not null)
        {
            host.SavedContainers.RemoveAll(c => c.Id == savedContainer.Id);
            configRepository.Save(config);
            logger.LogInformation("Deleted container '{Alias}' from server {ServerAlias}", alias, serverHost.Alias);
        }

        return true;
    }

    public List<ContainerModel> GetSavedByGroup(AppConfig config, string groupId)
    {
        var results = new List<ContainerModel>();

        foreach (var host in config.Hosts)
        {
            foreach (var container in host.SavedContainers)
            {
                if (container.GroupId == groupId)
                {
                    results.Add(container);
                }
            }
        }

        return results;
    }
}
