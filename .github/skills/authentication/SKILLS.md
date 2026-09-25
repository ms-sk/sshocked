# Authentication Methods

Each server supports a configurable authentication method:

| Method | Description |
|--------|-------------|
| **SSH Key** (default) | Uses OpenSSH default key resolution. Optionally specify an `IdentityFile` (`-i` flag). |
| **Password** | Launches an interactive SSH session allowing native password/passphrase prompt. |
| **SSH Agent** | Relies on `ssh-agent` for key resolution — no extra flags. |
| **Custom SSH Options** | Passes arbitrary `-o` flags (e.g., `-o StrictHostKeyChecking=no`). |

## Related Services

- `IProcessService` — SSH process execution with auth-aware argument building
