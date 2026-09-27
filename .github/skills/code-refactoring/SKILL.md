---
name: code-refactoring
description: Cleanup standards, type design, modularization, and acceptance criteria. Use when refactoring or cleaning up C# code.
---

# Code Refactoring & Cleanup Standards

## Code Minimization & Dead Code Removal

- Keep only strictly necessary execution logic.
- Remove unused methods, properties, fields, variables, and parameters.
- Remove unused `using` directives.

## Type & Class Design

- Eliminate all `partial` classes.
- Explicitly mark classes as `sealed` unless active class inheritance requires otherwise.
- Restructure files containing multiple type definitions so that each file contains **exactly one type definition**.
- **Use primary constructors** for all classes with constructor-injected dependencies. Replace private fields assigned from constructor parameters with primary constructor parameters. Example:

  ```csharp
  // Before:
  public sealed class MyService : IMyService
  {
      private readonly ILogger<MyService> _logger;
      private readonly IOtherService _other;

      public MyService(ILogger<MyService> logger, IOtherService other)
      {
          _logger = logger;
          _other = other;
      }

      public void DoWork() => _logger.LogInformation("...");
  }

  // After:
  public sealed class MyService(ILogger<MyService> logger, IOtherService other) : IMyService
  {
      public void DoWork() => logger.LogInformation("...");
  }
  ```

## File Length & Modularization

- Target file size: **under 300 lines** (absolute maximum: **400 lines**).
- Extract business or domain logic into dedicated service classes when file length limits are exceeded or responsibilities become cluttered.
- If you see the same logic repeated multiple times, extract it into a service class.

## Language & Documentation

- Use **English only** across all identifier names, log strings, and documentation.
- Strip out all code comments unless they document non-obvious domain complexities or critical constraints.
- If you see magic strings, create a static class for them.

## Async Naming Convention

- Methods that return `Task` or `Task<T>` **must not** have an `Async` suffix.
- The `Async` suffix is reserved for methods that are truly asynchronous (i.e., contain `await`). Methods that only return a `Task` without `await` still get no suffix.
- This applies to both interface declarations and implementations.

## Code Formatting

- Run a full, clean code auto-format across all modified files prior to completion.

## Acceptance Criteria

- All modified files contain exactly one type definition and do not exceed 400 lines of code.
- Zero unused imports, properties, or dead methods remain in the codebase.
- No `partial` keyword exists on non-generated classes; all uninherited classes carry the `sealed` modifier.
- No German terms or non-essential comments exist in code or metadata.
- Solution builds cleanly with no warnings regarding unused symbols.
- No `Async` suffix on any `Task`-returning method.
- All classes with constructor-injected dependencies use primary constructors (no separate private field + constructor body pattern).
