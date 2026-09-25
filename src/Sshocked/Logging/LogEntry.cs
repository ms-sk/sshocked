using Microsoft.Extensions.Logging;

namespace Sshocked.Logging;

public readonly struct LogEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public LogLevel LogLevel { get; init; }
    public string CategoryName { get; init; }
    public string Message { get; init; }
}
