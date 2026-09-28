using Spectre.Console;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public sealed class KeyboardShortcutService : IKeyboardShortcutService
{
    public MenuEntry? ShowMenu(string title, List<MenuEntry> options)
    {
        var prompt = new SelectionPrompt<MenuEntry>()
            .Title($"{title}")
            .PageSize(15)
            .UseConverter(entry => entry.DisplayLabel);

        prompt.AddChoices(options);

        prompt.HighlightStyle = Style.Parse("reverse");
        prompt.MoreChoicesText = "[grey](Move up and down to reveal more options)[/]";

        var selected = AnsiConsole.Prompt(prompt);

        return selected;
    }
}
