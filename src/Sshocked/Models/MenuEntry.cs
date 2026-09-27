namespace Sshocked.Models;

public sealed class MenuEntry
{
    public string Label { get; }
    public char? Shortcut { get; }
    public object? Tag { get; }
    public Func<Task>? ExecuteAsync { get; }
    public MenuActionType ActionType { get; }

    public MenuEntry(
        string label,
        char? shortcut = null,
        object? tag = null,
        Func<Task>? executeAsync = null,
        MenuActionType actionType = MenuActionType.Execute)
    {
        Label = label;
        Shortcut = shortcut is not null ? char.ToLowerInvariant(shortcut.Value) : null;
        Tag = tag;
        ExecuteAsync = executeAsync;
        ActionType = actionType;
    }

    public string DisplayLabel => Shortcut.HasValue
        ? $"[[{char.ToUpperInvariant(Shortcut.Value)}]] {Label}"
        : Label;
}

