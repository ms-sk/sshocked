using Microsoft.Extensions.Logging;

namespace Sshocked.Logging;

public sealed class FileLoggerProvider(FileLoggerProcessor processor) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, processor);
    }

    public void Dispose()
    {
        processor.Dispose();
    }
}
