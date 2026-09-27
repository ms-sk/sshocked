using Microsoft.Extensions.Logging;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class GroupManagementService(IConfigRepository configRepository, ILogger<GroupManagementService> logger) : IGroupManagementService
{
    public List<ServerGroup> GetAll()
    {
        var config = configRepository.Load();
        return config.Groups;
    }

    public ServerGroup Create(string name)
    {
        var config = configRepository.Load();

        var group = new ServerGroup
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name
        };

        config.Groups.Add(group);
        configRepository.Save(config);
        logger.LogInformation("Created group '{GroupName}' ({GroupId})", name, group.Id);

        return group;
    }

    public void Rename(string groupId, string newName)
    {
        var config = configRepository.Load();
        var group = config.Groups.FirstOrDefault(g => g.Id == groupId);

        if (group is null)
        {
            logger.LogWarning("Attempted to rename non-existent group {GroupId}", groupId);
            return;
        }

        group.Name = newName;
        configRepository.Save(config);
        logger.LogInformation("Renamed group {GroupId} to '{NewName}'", groupId, newName);
    }

    public void Delete(string groupId)
    {
        var config = configRepository.Load();
        var group = config.Groups.FirstOrDefault(g => g.Id == groupId);

        if (group is null)
        {
            logger.LogWarning("Attempted to delete non-existent group {GroupId}", groupId);
            return;
        }

        config.Groups.Remove(group);

        foreach (var host in config.Hosts.Where(h => h.GroupId == groupId))
        {
            host.GroupId = string.Empty;
        }

        configRepository.Save(config);
        logger.LogInformation("Deleted group '{GroupName}' ({GroupId})", group.Name, groupId);
    }

    public void AssignHost(string hostId, string groupId)
    {
        var config = configRepository.Load();
        var host = config.Hosts.FirstOrDefault(h => h.Id == hostId);

        if (host is null)
        {
            logger.LogWarning("Attempted to assign non-existent host {HostId}", hostId);
            return;
        }

        host.GroupId = groupId;
        configRepository.Save(config);
        logger.LogInformation("Assigned host '{Alias}' to group {GroupId}", host.Alias, groupId);
    }

    public List<ServerHost> GetHostsByGroup(string groupId)
    {
        var config = configRepository.Load();
        return config.Hosts.Where(h => h.GroupId == groupId).ToList();
    }
}
