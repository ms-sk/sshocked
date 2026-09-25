using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Sshocked.Logging;

public sealed class FileLoggerProcessor : IDisposable
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private const int MaxBackupFiles = 3;

    private readonly string _logDir;
    private readonly string _logFilePath;
    private readonly BlockingCollection<LogEntry> _queue = new(new ConcurrentQueue<LogEntry>());
    private readonly Thread _workerThread;
    private bool _disposed;

    public FileLoggerProcessor()
    {
        var baseDir = Environment.OSVersion.Platform == PlatformID.Win32NT
            ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");

        _logDir = Path.Combine(baseDir, "sshocked", "logs");
        Directory.CreateDirectory(_logDir);
        _logFilePath = Path.Combine(_logDir, "sshocked.log");

        _workerThread = new Thread(ProcessQueue)
        {
            IsBackground = true,
            Name = "FileLogger"
        };
        _workerThread.Start();
    }

    public void Enqueue(LogEntry entry)
    {
        if (!_disposed)
        {
            _queue.Add(entry);
        }
    }

    private void ProcessQueue()
    {
        foreach (var entry in _queue.GetConsumingEnumerable())
        {
            if (_disposed) break;

            try
            {
                RollIfNeeded();
                var line = FormatEntry(entry);
                File.AppendAllText(_logFilePath, line);
            }
            catch
            {
                // Swallow write errors — logging must never crash the app
            }
        }
    }

    private void RollIfNeeded()
    {
        if (!File.Exists(_logFilePath))
            return;

        var fileInfo = new FileInfo(_logFilePath);
        if (fileInfo.Length < MaxFileSizeBytes)
            return;

        // Delete the oldest backup
        var oldestBackup = Path.Combine(_logDir, $"sshocked.{MaxBackupFiles}.log");
        if (File.Exists(oldestBackup))
            File.Delete(oldestBackup);

        // Shift backups: .2 -> .3, .1 -> .2, etc.
        for (int i = MaxBackupFiles - 1; i >= 1; i--)
        {
            var src = Path.Combine(_logDir, $"sshocked.{i}.log");
            var dst = Path.Combine(_logDir, $"sshocked.{i + 1}.log");
            if (File.Exists(src))
                File.Move(src, dst, overwrite: true);
        }

        // Rename current log to .1
        File.Move(_logFilePath, Path.Combine(_logDir, "sshocked.1.log"), overwrite: true);
    }

    private static string FormatEntry(LogEntry entry)
    {
        var level = entry.LogLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRIT",
            _ => "UNKN"
        };

        return $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{level}] {entry.CategoryName}: {entry.Message}{Environment.NewLine}";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _queue.CompleteAdding();
        if (_workerThread.IsAlive)
        {
            _workerThread.Join(TimeSpan.FromSeconds(2));
        }
        _queue.Dispose();
    }
}
