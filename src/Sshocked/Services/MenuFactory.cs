using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class MenuFactory(IKeyboardShortcutService keyboardShortcut) : IMenuFactory
{
    public async Task<bool> RunMenu(string title, List<MenuEntry> entries)
    {
        var choice = keyboardShortcut.ShowMenu(title, entries);

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
