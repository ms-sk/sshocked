using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Sshocked.Interfaces;
using Sshocked.Services;

var services = new ServiceCollection();
ConfigureServices(services);

var serviceProvider = services.BuildServiceProvider();

AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
{
    var logger = serviceProvider.GetService<ILogger<Program>>();
    var ex = args.ExceptionObject as Exception;
    logger?.LogCritical(ex, "Unhandled application exception");
};

// Warm up Spectre.Console early — terminal profile detection can be slow
// on Windows Terminal / ConPTY. Doing it here while DI resolves is faster
// than on first menu interaction.
_ = AnsiConsole.Profile.Capabilities;

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application starting");

try
{
    var appRunner = serviceProvider.GetRequiredService<IAppRunner>();
    await appRunner.RunAsync();
    logger.LogInformation("Application shutdown complete");
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application terminated with unhandled exception");
    throw;
}
finally
{
    // Flush and dispose the logger processor before the service provider
    if (serviceProvider is IAsyncDisposable asyncDisposable)
    {
        await asyncDisposable.DisposeAsync();
    }
    else if (serviceProvider is IDisposable disposable)
    {
        disposable.Dispose();
    }
}

static void ConfigureServices(IServiceCollection services)
{
    services.AddLogging(builder =>
    {
        builder.AddFileLogger();
        builder.SetMinimumLevel(LogLevel.Trace);
    });

    services.AddSingleton<IConfigRepository, JsonConfigRepository>();
    services.AddSingleton<ISshConfigImporter, SshConfigImporter>();
    services.AddSingleton<IConsoleWriterService, ConsoleWriterService>();
    services.AddSingleton<IGroupManagementService, GroupManagementService>();
    services.AddSingleton<IServerCrudService, ServerCrudService>();
    services.AddSingleton<ISshRunnerService, SshRunnerService>();
    services.AddSingleton<INavigationService, NavigationService>();
    services.AddSingleton<IProcessService, ProcessService>();
    services.AddSingleton<IGroupMenuService, GroupMenuService>();
    services.AddSingleton<IServerMenuService, ServerMenuService>();
    services.AddSingleton<IMainMenuService, MainMenuService>();
    services.AddTransient<IAppRunner, AppRunner>();
}
