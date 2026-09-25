# sshocked

[![CI](https://github.com/ms-sk/sshocked/actions/workflows/ci.yml/badge.svg)](https://github.com/ms-sk/sshocked/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/ms-sk/sshocked?logo=github)](https://github.com/ms-sk/sshocked/releases)
[![License](https://img.shields.io/github/license/ms-sk/sshocked)](LICENSE)

**sshocked** – Shelling into servers and containers with lightning speed.

A .NET console application that provides a fast, interactive terminal UI for managing and connecting to SSH servers and containers. Organize hosts into groups, import from your OpenSSH config, and launch SSH sessions — all from a single, keyboard-driven interface.

---

## Features

- **Server Management** — Add, edit, and delete SSH hosts with aliases, addresses, ports, and authentication methods
- **Group Organization** — Group servers by project, environment, or any category; connect to all servers in a group at once
- **SSH Config Import** — Parse your existing `~/.ssh/config` and import hosts automatically
- **Multiple Auth Methods** — SSH key (with optional identity file), password, SSH agent, or custom SSH options
- **Interactive TUI** — Keyboard-driven terminal UI with sub-menu navigation and a server overview table
- **Native AOT** — Published as a native binary for fast startup and zero runtime dependencies

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 10 |
| UI Framework | [Spectre.Console](https://spectreconsole.net/) |
| DI | `Microsoft.Extensions.DependencyInjection` |
| Logging | `Microsoft.Extensions.Logging` (file-based, no console output) |
| Serialization | `System.Text.Json` (source-generated) |
| Publishing | Native AOT (`PublishAot`) |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (for development/building from source)
- [OpenSSH Client](https://learn.microsoft.com/en-us/windows-server/administration/openssh/openssh_install_firstuse) (for SSH connections)

## Quickstart

### Install from Release

1. Download the latest archive for your platform from the [Releases page](https://github.com/ms-sk/sshocked/releases)
2. Extract the binary and add it to your `PATH`

### Build from Source

```bash
# Clone the repository
git clone https://github.com/ms-sk/sshocked.git
cd sshocked

# Restore dependencies
dotnet restore

# Build
dotnet build --configuration Release

# Run
dotnet run --project src/Sshocked/Sshocked.csproj
```

### Publish Native Binary

```bash
dotnet publish src/Sshocked/Sshocked.csproj \
  -c Release \
  -r <RID> \
  --self-contained true \
  -o ./publish
```

Replace `<RID>` with your platform runtime identifier (e.g., `linux-x64`, `win-x64`, `osx-x64`, `linux-arm64`).

## Usage

Launch the application:

```bash
sshocked
```

The main menu displays a table of all configured servers grouped by category. Navigate using the keyboard:

- **Arrow keys** — Move selection
- **Enter** — Select a server or action
- **`[[B]] Back`** — Return to the previous menu
- **`[[E]] Exit`** — Exit the application

### Commands

| Action | Description |
|--------|-------------|
| **Connect** | Launch an interactive SSH session to the selected server |
| **Add Server** | Add a new SSH host (alias, host, port, user, auth method) |
| **Edit Server** | Modify an existing server's configuration |
| **Delete Server** | Remove a server from the configuration |
| **Create Group** | Create a new server group |
| **Import SSH Config** | Parse `~/.ssh/config` and import matching hosts |

See [SKILLS.md](SKILLS.md) for the full command reference and details on adding new commands.

## Configuration

Configuration is stored in a local JSON file:

| Platform | Path |
|----------|------|
| Windows | `%APPDATA%\sshocked\appconfig.json` |
| Linux/macOS | `~/.config/sshocked/appconfig.json` |

The file is auto-created on first run. Logs are written to `logs/sshocked.log` in the same directory (rotated at 5 MB, up to 3 backups).

## Project Structure

```
Sshocked.slnx              -- Solution file (.slnx format)
src/Sshocked/
├── Program.cs             -- Entry point with DI bootstrapping
├── Sshocked.csproj        -- Project file (net10.0, AOT-ready)
├── Models/                -- Domain models (AppConfig, ServerHost, ServerGroup)
├── Interfaces/            -- Core interfaces (IConfigRepository, ISshConfigImporter, IAppRunner, ...)
└── Services/              -- Implementations (JsonConfigRepository, AppRunner, ...)
```

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
