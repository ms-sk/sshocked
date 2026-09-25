using Microsoft.Extensions.Logging;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public class AppRunner : IAppRunner
{
    private readonly IConfigRepository _configRepository;
    private readonly ISshConfigImporter _sshConfigImporter;
    private readonly IMainMenuService _mainMenu;
    private readonly IConsoleWriterService _console;
    private readonly ILogger<AppRunner> _logger;

    public AppRunner(
        IConfigRepository configRepository,
        ISshConfigImporter sshConfigImporter,
        IMainMenuService mainMenu,
        IConsoleWriterService console,
        ILogger<AppRunner> logger)
    {
        _configRepository = configRepository;
        _sshConfigImporter = sshConfigImporter;
        _mainMenu = mainMenu;
        _console = console;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("AppRunner starting");

        var config = _configRepository.Load();

        if (config.IsFirstStart)
        {
            _console.WriteLine("Welcome to sshocked! Your configuration has been initialized.");
            _logger.LogInformation("First start detected");

            var sshConfigPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".ssh", "config");

            if (File.Exists(sshConfigPath))
            {
                _console.Write($"Found SSH config at {sshConfigPath}. Import hosts? (y/N): ");
                var response = _console.ReadLine()?.Trim().ToLowerInvariant();

                if (response is "y" or "yes")
                {
                    var importedHosts = _sshConfigImporter.Import();
                    config.Hosts.AddRange(importedHosts);
                    _console.WriteLine($"Imported {importedHosts.Count} host(s) from your SSH config.");
                    _logger.LogInformation("Imported {Count} host(s) from SSH config", importedHosts.Count);
                }
                else
                {
                    _console.WriteLine("Skipping SSH config import.");
                    _logger.LogInformation("User skipped SSH config import");
                }
            }

            config.IsFirstStart = false;
            _configRepository.Save(config);
        }

        await _mainMenu.ShowAsync();

        _logger.LogInformation("AppRunner finished");
    }
}
