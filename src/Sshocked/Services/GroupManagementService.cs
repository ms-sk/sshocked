using Microsoft.Extensions.Logging;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class GroupManagementService : IGroupManagementService
{
    private readonly IConfigRepository _configRepository;
    private readonly ILogger<GroupManagementService> _logger;

    public GroupManagementService(IConfigRepository configRepository, ILogger<GroupManagementService> logger)
    {
        _configRepository = configRepository;
        _logger = logger;
    }

    public List<ServerGroup> GetAll()
    {
        var config = _configRepository.Load();
        return config.Groups;
    }

    public ServerGroup Create(string name)
    {
        var config = _configRepository.Load();

        var group = new ServerGroup
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name
        };

        config.Groups.Add(group);
        _configRepository.Save(config);
        _logger.LogInformation("Created group '{GroupName}' ({GroupId})", name, group.Id);

        return group;
    }

    public void Rename(string groupId, string newName)
    {
        var config = _configRepository.Load();
        var group = config.Groups.FirstOrDefault(g => g.Id == groupId);

        if (group is null)
        {
            _logger.LogWarning("Attempted to rename non-existent group {GroupId}", groupId);
            return;
        }

        group.Name = newName;
        _configRepository.Save(config);
        _logger.LogInformation("Renamed group {GroupId} to '{NewName}'", groupId, newName);
    }

    public void Delete(string groupId)
    {
        var config = _configRepository.Load();
        var group = config.Groups.FirstOrDefault(g => g.Id == groupId);

        if (group is null)
        {
            _logger.LogWarning("Attempted to delete non-existent group {GroupId}", groupId);
            return;
        }

        config.Groups.Remove(group);

        foreach (var host in config.Hosts.Where(h => h.GroupId == groupId))
        {
            host.GroupId = string.Empty;
        }

        _configRepository.Save(config);
        _logger.LogInformation("Deleted group '{GroupName}' ({GroupId})", group.Name, groupId);
    }

    public void AssignHost(string hostId, string groupId)
    {
        var config = _configRepository.Load();
        var host = config.Hosts.FirstOrDefault(h => h.Id == hostId);

        if (host is null)
        {
            _logger.LogWarning("Attempted to assign non-existent host {HostId}", hostId);
            return;
        }

        host.GroupId = groupId;
        _configRepository.Save(config);
        _logger.LogInformation("Assigned host '{Alias}' to group {GroupId}", host.Alias, groupId);
    }

    public List<ServerHost> GetHostsByGroup(string groupId)
    {
        var config = _configRepository.Load();
        return config.Hosts.Where(h => h.GroupId == groupId).ToList();
    }
}
