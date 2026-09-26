using Spectre.Console;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class KeyboardShortcutService : IKeyboardShortcutService
{
    public MenuEntry? ShowMenu(string title, List<MenuEntry> options)
    {
        var shortcutMap = new Dictionary<char, MenuEntry>();
        foreach (var entry in options)
        {
            if (entry.Shortcut.HasValue && !shortcutMap.ContainsKey(entry.Shortcut.Value))
            {
                shortcutMap[entry.Shortcut.Value] = entry;
            }
        }

        var selectedIndex = 0;
        var menuStart = Console.CursorTop;
        RenderMenu(title, options, selectedIndex);

        while (true)
        {
            var key = System.Console.ReadKey(true);

            if (key.Key == ConsoleKey.UpArrow)
            {
                selectedIndex = selectedIndex > 0 ? selectedIndex - 1 : options.Count - 1;
                Console.SetCursorPosition(0, menuStart);
                RenderMenu(title, options, selectedIndex);
                continue;
            }

            if (key.Key == ConsoleKey.DownArrow)
            {
                selectedIndex = (selectedIndex + 1) % options.Count;
                Console.SetCursorPosition(0, menuStart);
                RenderMenu(title, options, selectedIndex);
                continue;
            }

            if (key.Key == ConsoleKey.Enter)
            {
                System.Console.WriteLine();
                return options[selectedIndex];
            }

            if (key.Key == ConsoleKey.Escape)
            {
                System.Console.WriteLine();
                return null;
            }

            if (shortcutMap.TryGetValue(char.ToLowerInvariant(key.KeyChar), out var selected))
            {
                System.Console.WriteLine();
                return selected;
            }
        }
    }

    private static void RenderMenu(string title, List<MenuEntry> options, int selectedIndex)
    {
        var lineCount = 2 + options.Count;
        for (var i = 0; i < lineCount; i++)
        {
            Console.Write(new string(' ', Console.WindowWidth - 1) + "\n");
        }
        Console.SetCursorPosition(0, Console.CursorTop - lineCount);

        AnsiConsole.MarkupLine($"{title}");
        AnsiConsole.WriteLine();

        for (var i = 0; i < options.Count; i++)
        {
            var entry = options[i];
            var prefix = i == selectedIndex ? "> " : "  ";

            if (i == selectedIndex)
            {
                AnsiConsole.MarkupLine($"{prefix}[reverse]{entry.DisplayLabel}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"{prefix}{entry.DisplayLabel}");
            }
        }

        var hint = "Arrow keys to navigate, shortcut key to select, Enter to confirm, Esc to cancel";
        AnsiConsole.Markup($"[grey]{hint}[/]");
        Console.Write(new string(' ', Math.Max(0, Console.WindowWidth - Console.CursorLeft)));
    }
}
