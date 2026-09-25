# Commands

Each command is implemented as a dedicated service interface + class.

## Current Commands

| Command | Service | Description |
|---------|---------|-------------|
| Main Menu | `IMainMenuService` | Renders the server table grouped by category and provides navigation |
| Server CRUD | `IServerCrudService` | Add, edit, and delete servers via interactive forms |
| SSH Runner | `ISshRunnerService` | Launch an interactive SSH session to a selected server |
| Group Management | `IGroupManagementService` | Create groups, assign hosts to groups |
| SSH Config Import | `ISshConfigImporter` | Parse `~/.ssh/config` and import hosts |
| Console I/O | `IConsoleWriterService` | Abstraction over `System.Console` for testability |
| Argument Parser | `IArgumentParserService` | Parse CLI arguments before TUI initialization |
| CLI Dispatcher | `ICliDispatcherService` | Handle direct connect, --list, --help, --version |

## Adding a New Command

1. Create the interface in `Interfaces/` (e.g., `IMyCommandService`)
2. Create the implementation in `Services/` (e.g., `MyCommandService`)
3. Register both in `Program.cs` `ConfigureServices()`
4. Add the command to the main menu in `MainMenuService`
