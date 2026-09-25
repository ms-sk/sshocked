using System.Text;
using Microsoft.Extensions.Logging;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public class ProcessService : IProcessService
{
    private readonly ILogger<ProcessService> _logger;

    public ProcessService(ILogger<ProcessService> logger)
    {
        _logger = logger;
    }

    public async Task RunSshAsync(ServerHost host)
    {
        var args = BuildSshArgs(host);

        _logger.LogInformation("Connecting to {Alias} via ssh {Args}", host.Alias, args);

        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ssh",
                Arguments = args,
                UseShellExecute = false
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            _logger.LogWarning("SSH session to {Alias} exited with code {ExitCode}", host.Alias, process.ExitCode);
        }
        else
        {
            _logger.LogInformation("SSH session to {Alias} completed", host.Alias);
        }
    }

    private static string BuildSshArgs(ServerHost host)
    {
        var sb = new StringBuilder();

        sb.Append("-p ");
        sb.Append(host.Port);

        switch (host.AuthType)
        {
            case AuthType.SshKey when !string.IsNullOrWhiteSpace(host.IdentityFile):
                sb.Append(" -i \"");
                sb.Append(host.IdentityFile);
                sb.Append('"');
                break;

            case AuthType.CustomConfig when !string.IsNullOrWhiteSpace(host.CustomSshOptions):
                sb.Append(' ');
                sb.Append(host.CustomSshOptions.Trim());
                break;

            case AuthType.Password:
            case AuthType.SshAgent:
            default:
                break;
        }

        sb.Append(' ');
        sb.Append(host.User);
        sb.Append('@');
        sb.Append(host.HostName);

        return sb.ToString();
    }
}