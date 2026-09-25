# sshocked Commands & Skills

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
| Navigation | `INavigationService` | View stack manager for sub-menu navigation |
| Process | `IProcessService` | SSH process execution with auth-aware argument building |
| Group Menu | `IGroupMenuService` | Group browsing and group detail sub-menus |
| Server Menu | `IServerMenuService` | Server selection and server action sub-menus |

## Navigation

The main menu uses a view stack for consistent sub-menu navigation:

| View | Description |
|------|-------------|
| **Main Menu** | Server overview table with top-level actions |
| **Group Detail** | Select a group → view its servers, connect all, or select a server |
| **Server Actions** | Select a server → Connect, Edit, Delete, or Back |

All sub-menus include a `[[B]] Back` option to return to the previous view. The Add Server form supports cancellation by leaving the Alias field empty. The Create Group form supports cancellation by leaving the name empty.

## Authentication Methods

Each server supports a configurable authentication method:

| Method | Description |
|--------|-------------|
| **SSH Key** (default) | Uses OpenSSH default key resolution. Optionally specify an `IdentityFile` (`-i` flag). |
| **Password** | Launches an interactive SSH session allowing native password/passphrase prompt. |
| **SSH Agent** | Relies on `ssh-agent` for key resolution — no extra flags. |
| **Custom SSH Options** | Passes arbitrary `-o` flags (e.g., `-o StrictHostKeyChecking=no`). |

## Adding a New Command

1. Create the interface in `Interfaces/` (e.g., `IMyCommandService`)
2. Create the implementation in `Services/` (e.g., `MyCommandService`)
3. Register both in `Program.cs` `ConfigureServices()`
4. Add the command to the main menu in `MainMenuService`
