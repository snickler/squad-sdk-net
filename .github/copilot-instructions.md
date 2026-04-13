# Copilot instructions for Squad.SDK.NET

## Project overview

Squad.SDK.NET is a .NET 10 library for multi-agent orchestration using GitHub Copilot. Single NuGet package (`Squad.SDK.NET`) with a DI entry point (`services.AddSquadSdk()`), sealed-by-default types, source-generated JSON serialization, and full AOT compatibility. Tests live in a separate xUnit project with access to internals via `InternalsVisibleTo`.

## Build, test, and pack

```shell
dotnet build Squad.SDK.NET.slnx -c Release
dotnet test Squad.SDK.NET.slnx -c Release
dotnet pack src/Squad.SDK.NET/Squad.SDK.NET.csproj -c Release -o ./artifacts
```

Run a single test by fully-qualified name:

```shell
dotnet test Squad.SDK.NET.slnx -c Release --filter "FullyQualifiedName~Squad.SDK.NET.Tests.SquadBuilderTests.Build_WithOnlyTeam_ProducesMinimalConfig"
```

There is no dedicated lint command. The build **is** the linter — `TreatWarningsAsErrors` is enabled globally in `Directory.Build.props` and `.editorconfig` rules emit warnings for style violations. A clean build means clean lint.

## Architecture at a glance

```
src/Squad.SDK.NET/          → The SDK library (net10.0, AOT-safe, IsAotCompatible=true)
  Abstractions/             → Public interfaces (ISquadClient, IAgentSession, ICoordinator, …)
  Builder/                  → SquadBuilder fluent API → produces SquadConfig
  Config/                   → Configuration records + ConfigJsonContext (source-gen)
  Coordinator/              → Request routing and fan-out
  Events/                   → Pub/sub event bus
  Extensions/               → ServiceCollectionExtensions (DI registration)
  Hooks/                    → Pre/post pipeline hooks
  Sharing/                  → Import/export + SharingJsonContext
  State/                    → SquadState + SquadStateJsonContext
  Storage/                  → IStorageProvider, InMemory, FileSystem implementations
  … (Agents, Casting, Skills, Runtime, Platform, Remote, etc.)

tests/Squad.SDK.NET.Tests/  → xUnit + Moq, mirrors src/ structure
samples/                    → Example apps
```

Central Package Management via `Directory.Packages.props` — never put version numbers in individual `.csproj` files.

## Non-obvious conventions

- **Sealed by default.** Every record, class, and event is `sealed` unless it must be extended.
- **Source-generated JSON only.** Three `JsonSerializerContext` subclasses exist (`ConfigJsonContext`, `SharingJsonContext`, `SquadStateJsonContext`). Never use `JsonSerializer.Serialize<T>()` without passing a context.
- **`ConfigureAwait(false)` in all library async code.** This is a library, not an app.
- **XML doc comments enforced in `src/`.** `GenerateDocumentationFile` is on globally; missing docs on public APIs produce build errors. The test project opts out (`GenerateDocumentationFile=false`).
- **Test naming:** `MethodName_Scenario_ExpectedResult`.
- **`ILogger<T>` everywhere.** `Console.WriteLine` is a bug.
- **Version source of truth** is `<Version>` in `src/Squad.SDK.NET/Squad.SDK.NET.csproj`. CI appends suffixes (`-ci.N`, `-dev`) for pre-release feeds.
- **No `nuget.config` in the repo.** If restore fails with "unable to find package GitHub.Copilot.SDK", add the GitHub Packages source locally:
  ```shell
  dotnet nuget add source "https://nuget.pkg.github.com/snickler/index.json" --name github --username YOUR_GH_USER --password YOUR_PAT
  ```
- **CI runs on both Ubuntu and Windows** (`ubuntu-latest` + `windows-latest` matrix in `.github/workflows/ci.yml`).

## AOT boundary: src vs tests

AOT rules (`IsAotCompatible=true`) apply only to `src/Squad.SDK.NET`. The test project is not AOT-published, so it may freely use reflection-based APIs (Moq, dynamic assertions, etc.). Never introduce `Activator.CreateInstance`, `Type.GetType(string)`, `dynamic`, or reflection-based serialization into `src/`.

## Defensive-copy rule for builders

Builder `Build()` methods must return a **new object** (defensive copy) rather than a live read-only wrapper over mutable builder state. Callers must be able to mutate the builder after `Build()` without affecting previously-built configs.

## Cross-platform path guidance

Use `Path.Combine()` or `Path.Join()` to build paths — never concatenate with a literal `/` or `\`. For path **validation**, do not rely on `Path.GetInvalidFileNameChars()`, `Path.DirectorySeparatorChar`, or `Path.AltDirectorySeparatorChar` — they return different values per OS. Instead, reject both `/` and `\` explicitly and use a portable Windows-superset invalid-character set (e.g. `<>:"/\|?*`) so validation is consistent regardless of host OS.

## GitHub CLI body encoding

When automating `gh pr create/edit` or `gh issue create`, avoid writing a JSON intermediate file that PowerShell re-encodes — this corrupts Unicode. Prefer inline `--body` for short ASCII content, or write a **UTF-8 (no BOM)** temp file and pass `--body-file`. Always verify the rendered PR/issue body after updating to catch encoding drift.

## CI-first debugging

When a failure appears only in CI (or differs across matrix legs), check the CI workflow output **before** attempting local reproduction. The Ubuntu/Windows matrix catches platform-specific issues — read both legs.
