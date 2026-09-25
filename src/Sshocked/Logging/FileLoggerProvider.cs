using Microsoft.Extensions.Logging;

namespace Sshocked.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly FileLoggerProcessor _processor;

    public FileLoggerProvider(FileLoggerProcessor processor)
    {
        _processor = processor;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _processor);
    }

    public void Dispose()
    {
        _processor.Dispose();
    }
}
