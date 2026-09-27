using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IMenuFactory
{
    /// <summary>
    /// Displays a menu and handles execution of the selected item's action.
    /// </summary>
    /// <param name="title">Menu title</param>
    /// <param name="entries">Menu entries with optional ExecuteAsync callbacks</param>
    /// <returns>True if the menu should continue, false if Exit/Back was selected</returns>
    Task<bool> RunMenu(string title, List<MenuEntry> entries);
}
