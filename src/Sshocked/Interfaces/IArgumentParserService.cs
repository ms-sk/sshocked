namespace Sshocked.Interfaces;

/// <summary>
/// Represents the result of parsing command-line arguments.
/// </summary>
public sealed record ParseResult
{
    /// <summary>True when a positional alias or group name was provided.</summary>
    public bool HasPositionalArg { get; init; }

    /// <summary>The raw positional argument value, if any.</summary>
    public string? PositionalArg { get; init; }

    /// <summary>True when --help or -h was passed.</summary>
    public bool ShowHelp { get; init; }

    /// <summary>True when --version or -v was passed.</summary>
    public bool ShowVersion { get; init; }

    /// <summary>True when --list or -l was passed.</summary>
    public bool ShowList { get; init; }

    /// <summary>True when no flags or positional args were provided (interactive mode).</summary>
    public bool IsInteractive => !HasPositionalArg && !ShowHelp && !ShowVersion && !ShowList;
}

public interface IArgumentParserService
{
    ParseResult Parse(string[] args);
}
