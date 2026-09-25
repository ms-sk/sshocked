# Copilot Instructions for sshocked

## Overview

sshocked is a .NET console application for shelling into servers and containers with lightning speed. It targets .NET 10 and uses the modern SDK-style project format with implicit usings and nullable reference types enabled.

## Build & Run

```bash
# Build the project
dotnet build

# Run the project
dotnet run --project src/Sshocked/Sshocked.csproj

# Restore packages
dotnet restore
```

## Project Structure

```
Sshocked.slnx          -- Solution file (new .slnx XML format)
src/Sshocked/          -- Console app project
  Program.cs           -- Entry point with DI bootstrapping
  Sshocked.csproj      -- Project file targeting net10.0
  Models/              -- Domain models (AppConfig, ServerHost, ServerGroup)
  Interfaces/          -- Core interfaces (IConfigRepository, ISshConfigImporter, IAppRunner)
  Services/            -- Implementations (JsonConfigRepository, AppRunner)
```

The solution uses the `.slnx` format (not the legacy `.sln`). The `.vscode/settings.json` sets `dotnet.defaultSolution` to `Sshocked.slnx`.

## Architecture

- **DI-first bootstrapping**: `Program.cs` uses `Microsoft.Extensions.DependencyInjection` to register services and resolve `IAppRunner` as the entry point.
- **IConfigRepository**: Persists `AppConfig` (groups, hosts, settings) to a local JSON file. On Windows the path is `%APPDATA%\sshocked\appconfig.json`; on Linux/macOS it's `~/.config/sshocked/appconfig.json`. The file is auto-created on first run.
- **ISshConfigImporter**: Parses the OpenSSH config file (not yet implemented).
- **IAppRunner**: Main application workflow, resolved from DI.
- **File logging**: Custom `ILoggerProvider` implementation writes to `%APPDATA%\sshocked\logs\sshocked.log` (Windows) or `~/.config/sshocked/logs/sshocked.log` (Linux/macOS). No console sink — logging never writes to stdout. Uses a background `BlockingCollection`-backed queue for non-blocking writes. Logs roll at 5 MB with up to 3 backup files.

## Commands

See [SKILLS.md](skills/SKILLS.md) for the full list of commands and how to add new ones.

## Conventions

- **Nullable reference types** are enabled (`<Nullable>enable</Nullable>`)
- **Implicit usings** are enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- The project is a top-level statement console app
- **JSON serialization** uses `System.Text.Json` source generators (`JsonSerializerContext`) for AOT/trimming compatibility
- **File-scoped namespaces** are used throughout
- **Primary constructors** are preferred for DI-based services
- Config file path is determined at runtime based on `Environment.OSVersion.Platform`
- **Service-per-responsibility**: Every new functionality gets its own service with a dedicated interface. Before implementing something, check existing services in `Services/` and interfaces in `Interfaces/` — if no existing service fits, create a new one. For example, console I/O goes through `IConsoleWriterService`, not direct `Console` calls.
