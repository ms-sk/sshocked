using Microsoft.Extensions.Logging;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public class SshConfigImporter : ISshConfigImporter
{
    private readonly ILogger<SshConfigImporter> _logger;

    public SshConfigImporter(ILogger<SshConfigImporter> logger)
    {
        _logger = logger;
    }

    public List<ServerHost> Import()
    {
        var sshConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".ssh", "config");

        if (!File.Exists(sshConfigPath))
        {
            _logger.LogInformation("No SSH config found at {Path}", sshConfigPath);
            return [];
        }

        _logger.LogInformation("Importing SSH config from {Path}", sshConfigPath);

        var hosts = new List<ServerHost>();
        var lines = File.ReadAllLines(sshConfigPath);

        string? currentAlias = null;
        string? currentHostName = null;
        string? currentUser = null;
        int currentPort = 22;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Skip comments and empty lines
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            // Split on first space or tab
            var parts = line.Split([' ', '\t'], 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
                continue;

            var keyword = parts[0].ToLowerInvariant();
            var value = parts[1];

            switch (keyword)
            {
                case "host":
                    // Save previous host block if complete
                    if (currentAlias is not null && currentHostName is not null)
                    {
                        hosts.Add(CreateHost(currentAlias, currentHostName, currentUser, currentPort));
                    }

                    currentAlias = value;
                    currentHostName = null;
                    currentUser = null;
                    currentPort = 22;
                    break;

                case "hostname":
                    currentHostName = value;
                    break;

                case "user":
                    currentUser = value;
                    break;

                case "port":
                    if (int.TryParse(value, out var port))
                        currentPort = port;
                    break;
            }
        }

        // Don't forget the last block
        if (currentAlias is not null && currentHostName is not null)
        {
            hosts.Add(CreateHost(currentAlias, currentHostName, currentUser, currentPort));
        }

        _logger.LogInformation("Imported {Count} host(s) from SSH config", hosts.Count);
        return hosts;
    }

    private static ServerHost CreateHost(string alias, string hostName, string? user, int port)
    {
        return new ServerHost
        {
            Id = Guid.NewGuid().ToString("N"),
            Alias = alias,
            HostName = hostName,
            User = user ?? Environment.UserName,
            Port = port,
            Tags = []
        };
    }
}
