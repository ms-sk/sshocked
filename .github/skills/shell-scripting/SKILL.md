---
name: shell-scripting
description: Standards for shell script structure, conciseness, error handling, and acceptance criteria. Use when writing or reviewing shell scripts.
---

# Shell Scripting Standards

## Code Minimization & Dead Code Removal

- Keep only strictly necessary execution logic.
- Remove unused variables, functions, and parameters.
- Remove redundant comments — let the code speak.

## Structure & Readability

- Use `set -eu` at the top of every script for strict error handling.
- Group related logic into small, single-purpose functions.
- Keep functions short — ideally under 15 lines each.
- Use consistent indentation (2 spaces).

## Conciseness

- Inline single-use variables where they don't hurt readability.
- Condense `case` statements to single-line patterns when patterns are short.
- Combine `echo` + `exit 1` into a single `die()` function.
- Use `&&` / `||` for simple conditionals instead of full `if` blocks.
- Prefer `printf` over `echo` for portability.

## Error Handling

- Every script must have a `die()` function that prints to stderr and exits non-zero.
- Validate prerequisites (root, dependencies) at the top before doing any work.
- Use `trap` to clean up temporary files on exit.
- Fail fast with clear, human-readable error messages.

## Language & Documentation

- Use **English only** for all messages, variable names, and comments.
- Strip out all non-essential comments.
- Keep the shebang line (`#!/bin/sh` or `#!/bin/bash`) — that's the only required header.

## Acceptance Criteria

- Script runs with `set -eu` and no warnings from shellcheck.
- Zero unused variables or dead code.
- All error messages are clear and actionable.
- Temporary files are cleaned up on success and failure.
- Script is POSIX-compliant unless bash-specific features are explicitly required.
