namespace Sshocked.Models;

public sealed class MenuEntry
{
    public string Label { get; }
    public char? Shortcut { get; }

    public MenuEntry(string label, char? shortcut = null)
    {
        Label = label;
        Shortcut = shortcut is not null ? char.ToLowerInvariant(shortcut.Value) : null;
    }

    public string DisplayLabel => Shortcut.HasValue
        ? $"[[{char.ToUpperInvariant(Shortcut.Value)}]] {Label}"
        : Label;
}
