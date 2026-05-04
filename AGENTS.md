# AGENTS.md

## Purpose

This document provides guidance for AI agents and contributors working on the **TypedConfig** repository.

TypedConfig is a small F# library for loading configuration from environment variables into strongly typed F# records with validation.

---

## Commands

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run a single test file
dotnet test --filter "FullyQualifiedName~TypedConfig.Tests.ParserTests"

# Check for warnings (treat as errors)
dotnet build -warnaserror

# Restore Paket dependencies
dotnet paket restore

# Format code
dotnet fantomas .

# Lint (if configured)
dotnet fsharplint lint TypedConfig/TypedConfig.fsproj
```

> Always run `dotnet build` and `dotnet test` before committing. All tests must pass.

---

## Project Structure

```
TypedConfig/
├── TypedConfig/
│   ├── TypedConfig.fsproj
│   ├── TypedConfig.fs
│   └── paket.references
├── TypedConfig.Tests/
│   ├── TypedConfig.Tests.fsproj
│   ├── Tests.fs
│   └── paket.references
├── TypedConfig.slnx
├── paket.dependencies
├── paket.lock
├── build.sh
├── AGENTS.md
└── README.md
```

---

## Design Goals

- Keep the library **small and focused**
- Prefer **clarity over cleverness**
- Follow **idiomatic F# patterns**
- Ensure **type safety** and **explicit error handling**
- Avoid unnecessary dependencies
- Make features **incremental and composable**

---

## Core Concepts

### 1. Typed Configuration

Configuration is defined as an F# record:

```fsharp
type Config =
    {
        DATABASE_URL: string
        PORT: int
        DEBUG: bool
    }
```

The library maps external configuration values into this record.

---

### 2. Result-Based Error Handling

All operations return:

```fsharp
Result<'T, ConfigError list>
```

- Errors must be **accumulated**, not fail-fast
- Avoid exceptions for normal control flow

---

### 3. Separation of Concerns

The architecture should remain split into:

- **Source** → where values come from
- **Parser** → how strings become typed values
- **Builder** → how records are constructed

Do not mix these responsibilities.

---

### 4. Reflection Usage

Reflection is used to:

- Inspect record fields
- Determine field names and types
- Dynamically construct records

Guidelines:

- Use `FSharpType` and `FSharpValue`
- Avoid overusing reflection outside the core builder
- Keep reflection logic isolated

---

## Configuration Sources

Configuration should remain easy to extend with new sources, but the current implementation reads environment variables only.

Current reader behavior:

```fsharp
Reader.tryGet : string -> string option
```

`Api.load<'T> ()` inspects the target record fields and reads matching environment variables by field name.

Future sources such as `.env` files, JSON, or CLI args should be added incrementally without mixing parsing and record-building logic.

Agents should prefer adding a small provider/source layer when extending sources instead of embedding source-specific logic into `Parser` or `Builder`.

---

## Parsing Rules

- Parsing must be **explicit and type-driven**

- Supported types (initial scope):
  - `string`
  - `int`
  - `bool`

- Invalid values must return:

```fsharp
InvalidValue (name, value, expectedType)
```

- Unsupported field types must return:

```fsharp
UnsupportedType typeName
```

Do not silently ignore or coerce values.

---

## Error Model

```fsharp
type ConfigError =
    | MissingVariable of name: string
    | InvalidValue of name: string * value: string * expected: string
    | UnsupportedType of type': string
```

Rules:

- Always return all errors
- Do not stop on first failure
- Keep error values clear and minimal
- Current invalid values include the raw invalid value; do not add logging or additional exposure of secrets

---

## Coding Guidelines

- Prefer **pure functions**
- Avoid mutable state
- Use **explicit types** where helpful
- Keep functions small and composable
- Avoid premature abstraction

---

## Testing Expectations

Every feature should include tests:

- Successful parsing
- Missing variables
- Invalid values
- Multiple errors (accumulation)

Use deterministic inputs. Avoid reliance on real environment state where possible.

Current tests isolate environment variables by saving previous values, setting test values, and restoring them in `finally`.

---

## Security

- **Never** hardcode secrets, API keys, or connection strings in source code or tests
- Use controlled environment variable setup for sensitive values in tests; never inline real secrets
- Do not log or expose raw configuration values in error messages
- Future `.env` files used in tests must contain only dummy/placeholder values and must not be committed with real credentials

---

## PR Checklist

Before opening a pull request, verify:

- [ ] `dotnet build` passes with no warnings
- [ ] `dotnet test` passes (all tests green)
- [ ] New or changed behavior is covered by tests
- [ ] No breaking changes to public API (or clearly justified and documented)
- [ ] Code follows existing patterns (pure functions, Result-based errors, SoC)
- [ ] No new dependencies introduced without discussion

Commit message format: `<type>: <short description>`
Examples: `feat: add option field support`, `fix: accumulate errors in builder`, `test: cover invalid bool parsing`

---

## Agent Permissions

| Action                             | Allowed without asking |
| ---------------------------------- | ---------------------- |
| Read files, list structure         | ✅ Yes                 |
| Run `dotnet build` / `dotnet test` | ✅ Yes                 |
| Edit existing `.fs` files          | ✅ Yes                 |
| Add new `.fs` files                | ✅ Yes                 |
| Add new NuGet dependencies         | ❌ Ask first           |
| Delete files                       | ❌ Ask first           |
| Modify `.fsproj` / solution files  | ❌ Ask first           |
| Modify Paket dependency files      | ❌ Ask first           |
| Refactor across multiple modules   | ❌ Ask first           |

---

## Non-Goals (for now)

- No complex configuration frameworks
- No dependency injection
- No automatic magic beyond reflection-based record binding
- No deep nesting or advanced type support in initial versions

---

## Future Extensions

Planned areas:

- Optional fields (`option`)
- Custom field names (attributes)
- Enum parsing
- Nested records
- Additional configuration sources

These should be added incrementally without breaking existing behavior.

---

## Contribution Guidelines for Agents

When modifying or extending the code:

1. Do not introduce breaking changes without clear justification
2. Keep API surface minimal
3. Prefer extending via composition over rewriting core logic
4. Maintain consistency with existing patterns
5. Add or update tests with every change

---

## Summary

TypedConfig is intended to be:

- Simple
- Predictable
- Type-safe
- Easy to understand and extend

Avoid overengineering. Prioritize readability and correctness.
