using Sshocked.Interfaces;

namespace Sshocked.Services;

public class ConsoleWriterService : IConsoleWriterService
{
    public void WriteLine(string message) => Console.WriteLine(message);
    public void Write(string message) => Console.Write(message);
    public string? ReadLine() => Console.ReadLine();
}
