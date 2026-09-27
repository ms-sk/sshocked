using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IMenuFactory
{
    Task<bool> RunMenu(string title, List<MenuEntry> entries);
}
