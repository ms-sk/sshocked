using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public class NavigationService : INavigationService
{
    private readonly Stack<ViewType> _stack = new();

    public ViewType Current => _stack.Count > 0 ? _stack.Peek() : ViewType.MainMenu;
    public bool IsRoot => _stack.Count == 0;

    public void Push(ViewType view) => _stack.Push(view);
    public void Pop()
    {
        if (_stack.Count > 0) _stack.Pop();
    }
    public void PopToRoot() => _stack.Clear();
}