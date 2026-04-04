---
name: sdk-conventions
description: Squad.SDK.NET codebase conventions — sealed records, source-gen JSON, AOT-safe, no reflection
domain: coding-standards
confidence: high
source: established for Squad.SDK.NET
---

## Context

Squad.SDK.NET is a .NET library for multi-agent orchestration. It must be AOT-compatible, have minimal dependencies, and follow modern .NET patterns.

## Architecture Conventions

### Project Structure
```
src/Squad.SDK.NET/
  Abstractions/     — Public interfaces (ISquadClient, IAgentSession, etc.)
  Agents/           — Agent lifecycle management
  Builder/          — SquadClientBuilder (fluent API)
  Casting/          — Character casting system
  Config/           — Configuration models
  Coordinator/      — Work coordination and routing
  Events/           — Event bus (pub/sub)
  Extensions/       — ServiceCollectionExtensions
  Hooks/            — Pipeline hooks
  Marketplace/      — Agent marketplace
  Platform/         — Platform detection
  Remote/           — Remote transport
  Resolution/       — Dependency resolution
  Roles/            — Role definitions
  Runtime/          — Runtime management
  Sharing/          — State sharing
  Skills/           — Skill system
  State/            — State management
  Storage/          — Persistence
  Tools/            — Tool integration
  Utils/            — Utilities

tests/Squad.SDK.NET.Tests/
  (mirrors src/ structure)
```

### Type Patterns
- **DTOs and value types:** `sealed record` with primary constructors
- **Services:** `sealed class` implementing an `I{Name}` interface
- **Builders:** Fluent API with method chaining returning `this`
- **Events:** `sealed record` extending a base event type
- **Configuration:** `sealed record` with sensible defaults

### JSON Serialization
- **Always** use source-generated `JsonSerializerContext`
- **Never** use `JsonSerializer.Serialize<T>()` without a context
- Each module may have its own `JsonSerializerContext` if needed
- Register `[JsonSerializable(typeof(T))]` for every serialized type

### Dependency Injection
- All services registered in `ServiceCollectionExtensions`
- Singletons for stateful services (e.g., `SquadClient`)
- Transient for stateless services
- Use `ILogger<T>` everywhere — inject via `ILoggerFactory`

### Error Handling
- Use `try/catch` with `ILogger.LogError(ex, "message")`
- Never swallow exceptions silently
- Return result types for operations that can fail predictably

## AOT Compatibility Rules

1. **No `Activator.CreateInstance`** — use factory delegates
2. **No reflection-based serialization** — use source-gen JSON
3. **No `Type.GetType(string)`** — use compile-time type references
4. **No `dynamic`** — use strongly typed interfaces
5. **`PublishAot=true` must produce 0 IL warnings** from project code

## Testing Conventions

- **Framework:** xUnit
- **Mocking:** Moq
- **Assertions:** FluentAssertions (or raw xUnit Assert)
- **Naming:** `MethodName_Scenario_ExpectedResult`
- **Coverage:** Every public method has at least one test
- **Build command:** `dotnet test Squad.SDK.NET.slnx`

## Build & Publish

- **Build:** `dotnet build Squad.SDK.NET.slnx`
- **Test:** `dotnet test Squad.SDK.NET.slnx`
- **Pack:** `dotnet pack src/Squad.SDK.NET/Squad.SDK.NET.csproj -c Release`
- **Target:** `net10.0`
- **Central Package Management:** `Directory.Packages.props`
