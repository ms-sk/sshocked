using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface INavigationService
{
    ViewType Current { get; }
    bool IsRoot { get; }
    void Push(ViewType view);
    void Pop();
    void PopToRoot();
}