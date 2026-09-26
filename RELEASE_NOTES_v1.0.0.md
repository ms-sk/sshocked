# sshocked v1.0.0 — Initial Release

**Shell into servers and containers with lightning speed.**

After months of development, sshocked reaches its first stable release. This is a complete rewrite of server management for the terminal — a keyboard-driven TUI for organizing, discovering, and connecting to SSH hosts.

## Installation

```bash
# Linux / macOS
curl -fsSL https://github.com/ms-sk/sshocked/releases/latest/download/install.sh | sudo sh

# Windows (PowerShell as Administrator)
irm https://github.com/ms-sk/sshocked/releases/latest/download/install.ps1 | iex

# Debian / Ubuntu
sudo apt install ./sshocked-linux-x64.deb
```

Or download the archive for your platform from the assets below.

## Features

### Server Management
- Add, edit, and delete SSH hosts with aliases, addresses, ports, and user
- Organize servers into groups by project, environment, or any category
- Connect to all servers in a group at once
- Import hosts from your existing `~/.ssh/config`

### Authentication Methods
- SSH key authentication with optional identity file
- Password-based authentication
- SSH agent forwarding
- Custom SSH options passthrough

### Interactive Terminal UI
- Keyboard-driven navigation with fuzzy search
- Server overview table grouped by category
- Sub-menu navigation with consistent back action
- Direct connection via CLI arguments: `ssk <alias>`, `ssk <hostname>`, `ssk <group>`

### Non-Interactive Mode
- `ssk --list` / `ssk -l` — list all configured servers and groups (pipe-friendly)
- `ssk --help` / `ssk -h` — usage guide
- `ssk --version` / `ssk -v` — version information

### Technical Highlights
- **Native AOT** — compiled to native binary for instant startup and zero runtime dependencies
- **Cross-platform** — Linux (x64, arm64), Windows (x64), macOS (x64)
- **File-based logging** — automatic log rotation at 5 MB, up to 3 backups
- **DI-first architecture** — built with `Microsoft.Extensions.DependencyInjection`

## What's Included

### Core Features
- #1 — Setup DI Container & Application Bootstrapper
- #2 — Data Model & JSON Repository
- #3 — First-Start Wizard & SSH Config Importer
- #4 — Main Menu & Server Group Management
- #6 — Single Server Connection Execution
- #8 — Lightweight File & Diagnostics Logging
- #10 — Interactive Server CRUD Management
- #12 — Server Authentication Methods Support
- #13 — Sub-Menu Navigation & Universal Back Action

### Quality of Life
- #5 — Fuzzy Search for Servers & Groups
- #14 — Fix Issues in Pipeline
- #15 — Create Project README with Status Badges & Release Link
- #16 — Code Refactoring & Cleanup Standards

### Infrastructure
- #11 — GitHub Actions CI/CD & Automated Release Pipeline
- #18 — CLI Argument Parsing & Direct SSH Execution
- #17 — Choose a License (GPLv3)
- #19 — Installation Comfort (one-line scripts, .deb packages, Windows support)

## License

GNU General Public License v3.0 — see [LICENSE](LICENSE).
