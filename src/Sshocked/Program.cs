using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    // Flush and dispose the logger processor
    if (serviceProvider is IDisposable disposable)
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
    services.AddTransient<IAppRunner, AppRunner>();
}
