using Microsoft.Extensions.Logging;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class CliDispatcherService : ICliDispatcherService
{
    private readonly IConfigRepository _configRepository;
    private readonly ISshRunnerService _sshRunner;
    private readonly ILogger<CliDispatcherService> _logger;

    public CliDispatcherService(
        IConfigRepository configRepository,
        ISshRunnerService sshRunner,
        ILogger<CliDispatcherService> logger)
    {
        _configRepository = configRepository;
        _sshRunner = sshRunner;
        _logger = logger;
    }

    public async Task<bool> Dispatch(ParseResult parseResult)
    {
        if (parseResult.ShowHelp)
        {
            PrintHelp();
            return true;
        }

        if (parseResult.ShowVersion)
        {
            PrintVersion();
            return true;
        }

        if (parseResult.ShowList)
        {
            PrintList();
            return true;
        }

        if (parseResult.HasPositionalArg && parseResult.PositionalArg is not null)
        {
            return await ConnectToTarget(parseResult.PositionalArg);
        }

        return false;
    }

    private async Task<bool> ConnectToTarget(string target)
    {
        var config = _configRepository.Load();

        // Try matching as a server alias (case-insensitive)
        var host = config.Hosts.FirstOrDefault(h =>
            h.Alias.Equals(target, StringComparison.OrdinalIgnoreCase));

        if (host is not null)
        {
            _logger.LogInformation("Direct connect to server '{Alias}'", host.Alias);
            await _sshRunner.Connect(host);
            return true;
        }

        // Try matching as a hostname (case-insensitive)
        host = config.Hosts.FirstOrDefault(h =>
            h.HostName.Equals(target, StringComparison.OrdinalIgnoreCase));

        if (host is not null)
        {
            _logger.LogInformation("Direct connect to hostname '{HostName}'", host.HostName);
            await _sshRunner.Connect(host);
            return true;
        }

        // Try matching as a group name (case-insensitive)
        var group = config.Groups.FirstOrDefault(g =>
            g.Name.Equals(target, StringComparison.OrdinalIgnoreCase));

        if (group is not null)
        {
            var groupHosts = config.Hosts.Where(h => h.GroupId == group.Id).ToList();
            if (groupHosts.Count == 0)
            {
                Console.WriteLine($"Group '{group.Name}' has no servers.");
                return true;
            }

            _logger.LogInformation("Direct connect to group '{GroupName}' ({Count} hosts)", group.Name, groupHosts.Count);

            foreach (var groupHost in groupHosts)
            {
                Console.WriteLine($"Connecting to {groupHost.Alias} ({groupHost.HostName})...");
                await _sshRunner.Connect(groupHost);
            }

            return true;
        }

        Console.WriteLine($"Error: No server or group found matching '{target}'.");
        Console.WriteLine("Run 'ssk --help' for usage information.");
        return true;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("sshocked - Shell into servers and containers with lightning speed");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  ssk                        Launch the interactive TUI");
        Console.WriteLine("  ssk <alias>                Connect directly to a server by alias");
        Console.WriteLine("  ssk <hostname>             Connect directly to a server by hostname");
        Console.WriteLine("  ssk <group>                Connect to all servers in a group");
        Console.WriteLine("  ssk --list, -l             List all configured servers and groups");
        Console.WriteLine("  ssk --help, -h             Show this help message");
        Console.WriteLine("  ssk --version, -v          Show version information");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  ssk prod-db-01             Connect to server 'prod-db-01'");
        Console.WriteLine("  ssk production             Connect to all servers in group 'production'");
    }

    private static void PrintVersion()
    {
        var version = typeof(CliDispatcherService).Assembly.GetName().Version;
        Console.WriteLine($"sshocked version {version?.ToString(3) ?? "0.0.0"}");
    }

    private void PrintList()
    {
        var config = _configRepository.Load();

        if (config.Groups.Count == 0 && config.Hosts.Count == 0)
        {
            Console.WriteLine("No servers configured.");
            return;
        }

        // Print groups
        if (config.Groups.Count > 0)
        {
            Console.WriteLine("Groups:");
            foreach (var group in config.Groups)
            {
                var count = config.Hosts.Count(h => h.GroupId == group.Id);
                Console.WriteLine($"  {group.Name} ({count} server{(count == 1 ? "" : "s")})");
            }
            Console.WriteLine();
        }

        // Print servers
        if (config.Hosts.Count > 0)
        {
            Console.WriteLine("Servers:");
            foreach (var host in config.Hosts)
            {
                var groupName = config.Groups
                    .FirstOrDefault(g => g.Id == host.GroupId)?.Name ?? "(uncategorized)";
                Console.WriteLine($"  {host.Alias,-20} {host.User}@{host.HostName}:{host.Port}  [{groupName}]");
            }
        }
    }
}
