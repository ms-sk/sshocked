using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IKeyboardShortcutService
{
    MenuEntry? ShowMenu(string title, List<MenuEntry> options);
}
