using Sshocked.Interfaces;

namespace Sshocked.Services;

public sealed class ArgumentParserService : IArgumentParserService
{
    public ParseResult Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return new ParseResult();
        }

        var first = args[0].ToLowerInvariant();

        // Check for flags first
        if (first is "--help" or "-h")
        {
            return new ParseResult { ShowHelp = true };
        }

        if (first is "--version" or "-v")
        {
            return new ParseResult { ShowVersion = true };
        }

        if (first is "--list" or "-l")
        {
            return new ParseResult { ShowList = true };
        }

        // Anything else is treated as a positional argument (alias or group name)
        return new ParseResult
        {
            HasPositionalArg = true,
            PositionalArg = args[0]
        };
    }
}
