using Microsoft.Extensions.Logging;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class SshRunnerService : ISshRunnerService
{
    private readonly IProcessService _processService;
    private readonly ILogger<SshRunnerService> _logger;

    public SshRunnerService(IProcessService processService, ILogger<SshRunnerService> logger)
    {
        _processService = processService;
        _logger = logger;
    }

    public async Task ConnectAsync(ServerHost host)
    {
        _logger.LogInformation("Connecting to {Alias}", host.Alias);
        await _processService.RunSshAsync(host);
    }
}
