namespace Sshocked.Models;

public sealed class MenuEntry
{
    public string Label { get; }
    public char? Shortcut { get; }
    public object? Tag { get; }

    public MenuEntry(string label, char? shortcut = null, object? tag = null)
    {
        Label = label;
        Shortcut = shortcut is not null ? char.ToLowerInvariant(shortcut.Value) : null;
        Tag = tag;
    }

    public string DisplayLabel => Shortcut.HasValue
        ? $"[[{char.ToUpperInvariant(Shortcut.Value)}]] {Label}"
        : Label;
}
