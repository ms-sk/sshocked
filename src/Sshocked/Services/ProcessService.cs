using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class ProcessService(ILogger<ProcessService> logger) : IProcessService
{
    public async Task RunSsh(ServerHost host)
    {
        var args = BuildSshArgs(host);

        logger.LogInformation("Connecting to {Alias} via ssh {Args}", host.Alias, args);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
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
                    logger.LogWarning("SSH session to {Alias} exited with code {ExitCode}", host.Alias, process.ExitCode);
        }
        else
        {
                    logger.LogInformation("SSH session to {Alias} completed", host.Alias);
        }
    }

    public async Task<string> RunSshCommand(ServerHost host, string command)
    {
            logger.LogInformation("Running remote command on {Alias}: {Command}", host.Alias, command);

        var psi = new ProcessStartInfo
        {
            FileName = "ssh",
            Arguments = $"{BuildSshArgs(host)} -- {command}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                outputBuilder.AppendLine(args.Data);
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                errorBuilder.AppendLine(args.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        var output = outputBuilder.ToString();
        var error = errorBuilder.ToString();

        if (process.ExitCode != 0)
        {
                    logger.LogWarning("Remote command on {Alias} exited with code {ExitCode}: {Error}",
                host.Alias, process.ExitCode, error);
        }

        if (!string.IsNullOrEmpty(error))
        {
            output += error;
        }

        return output;
    }

    public async Task RunSshInteractive(ServerHost host, string command)
    {
        var sshArgs = BuildSshArgs(host);

            logger.LogInformation("Running interactive remote command on {Alias}: {Command}", host.Alias, command);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ssh",
                Arguments = $"{sshArgs} -t -- {command}",
                UseShellExecute = false
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
                    logger.LogWarning("Interactive remote command on {Alias} exited with code {ExitCode}", host.Alias, process.ExitCode);
        }
    }

    internal static string BuildSshArgs(ServerHost host)
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