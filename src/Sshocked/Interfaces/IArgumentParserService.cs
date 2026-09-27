namespace Sshocked.Interfaces;

public interface IArgumentParserService
{
    ParseResult Parse(string[] args);
}
