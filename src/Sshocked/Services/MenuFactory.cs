using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class MenuFactory : IMenuFactory
{
    private readonly IKeyboardShortcutService _keyboardShortcut;

    public MenuFactory(IKeyboardShortcutService keyboardShortcut)
    {
        _keyboardShortcut = keyboardShortcut;
    }

    public async Task<bool> RunMenu(string title, List<MenuEntry> entries)
    {
        var choice = _keyboardShortcut.ShowMenu(title, entries);

        if (choice?.ActionType == MenuActionType.Exit || choice?.ActionType == MenuActionType.Back)
        {
            return false;
        }

        if (choice?.ExecuteAsync != null)
        {
            await choice.ExecuteAsync();
        }

        return true;
    }
}
