namespace Sshocked.Interfaces;

public interface IConsoleWriterService
{
    void WriteLine(string message);
    void Write(string message);
    string? ReadLine();
}
