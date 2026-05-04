# TypedConfig

TypedConfig is a lightweight F# library for loading configuration into strongly typed records with validation.

It maps environment variables into F# record types, ensuring type safety and clear error reporting.

## Features

* Load configuration into F# records using generics
* Built-in parsing for common types (`string`, `int`, `bool`)
* Accumulates validation errors instead of failing fast
* Environment variable source support
* Small API surface with explicit `Result`-based errors

## Requirements

* .NET 10 SDK
* Paket for dependency restore

## Example

```fsharp
open TypedConfig

type Config =
    {
        DATABASE_URL: string
        PORT: int
        DEBUG: bool
    }

let result =
    Api.load<Config> ()
```

If all required environment variables are present and valid, the result is:

```fsharp
Ok {
    DATABASE_URL = "localhost"
    PORT = 5000
    DEBUG = true
}
```

or

```fsharp
Error [
    MissingVariable "DATABASE_URL"
    InvalidValue ("PORT", "abc", "int")
    UnsupportedType "DateTime"
]
```

## Supported Types

TypedConfig currently supports these record field types:

* `string`
* `int`
* `bool`

Unsupported field types return an `UnsupportedType` error.

## Development

```bash
dotnet build
dotnet test
```

## NuGet Packaging and Publishing

Package metadata is defined in `TypedConfig/TypedConfig.fsproj` for SDK-style packing.

Publishing is automated by GitHub Actions via `.github/workflows/nuget-publish.yml`.

Release flow:

1. Ensure GitHub secret `NUGET_API_KEY` is configured in the repository settings.
2. Create and push a version tag in format `*.*.*` (for example `0.1.0`).
3. Workflow restores, builds, tests, packs, and publishes the package to NuGet.org.

Example:

```bash
git tag 0.1.0
git push origin 0.1.0
```

The repository uses Paket files:

* `paket.dependencies`
* `paket.lock`
* `TypedConfig/paket.references`
* `TypedConfig.Tests/paket.references`

## Motivation

TypedConfig helps eliminate boilerplate configuration code and runtime errors caused by missing or invalid values, while keeping the API simple and idiomatic for F# developers.

## Status

Early stage. API may change. Planned future extensions include additional sources, optional fields, custom field names, enum parsing, and nested records.
