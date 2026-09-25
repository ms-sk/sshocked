using Microsoft.Extensions.Logging;

namespace Sshocked.Logging;

public sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly FileLoggerProcessor _processor;

    public FileLogger(string categoryName, FileLoggerProcessor processor)
    {
        _categoryName = categoryName;
        _processor = processor;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        if (exception is not null)
        {
            message = $"{message}{Environment.NewLine}{exception}";
        }

        _processor.Enqueue(new LogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            LogLevel = logLevel,
            CategoryName = _categoryName,
            Message = message
        });
    }
}
