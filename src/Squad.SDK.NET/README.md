# Squad.SDK.NET

[![CI](https://github.com/snickler/squad-sdk-net/actions/workflows/ci.yml/badge.svg)](https://github.com/snickler/squad-sdk-net/actions/workflows/ci.yml)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Multi-agent orchestration SDK for .NET 10, wrapping GitHub.Copilot.SDK with fluent configuration and advanced routing.

## Overview

Squad.SDK.NET is a .NET port of [@bradygaster/squad-sdk](https://github.com/bradygaster/squad-sdk), designed to orchestrate teams of AI agents using the GitHub Copilot SDK. It provides a fluent builder API for defining agent charters, routing rules, and governance policies — with built-in support for session pooling, event pub/sub, cost tracking, and tool access control.

## Features

- **Fluent builder API** for squad configuration with chainable methods
- **Coordinator with intelligent routing** — work-type matching, priority-based dispatch, and fan-out
- **Event bus (pub/sub)** via `System.Threading.Channels` for decoupled event-driven architecture
- **Pre/post tool-use hook pipeline** for governance, policy enforcement, and auditing
- **Agent session management** with pooling and per-agent lifecycle tracking
- **Cost tracking and usage aggregation** across sessions and models
- **Charter compiler** — parses markdown + YAML frontmatter into `AgentCharter` objects
- **Skill registry and loader** for extensible agent capabilities
- **Skill security scanner** — static analysis of skill markdown for credentials, exec patterns, and privilege escalation (ports upstream security-review patterns)
- **Platform detection** — OS, terminal, and IDE awareness
- **Import/export** for sharing squad configurations as portable JSON
- **Full AOT / Native AOT compatibility** — zero reflection, zero dynamic code generation
- **Source-generated JSON serialization** via four dedicated `JsonSerializerContext` implementations
- **Microsoft.Extensions.DependencyInjection integration** — one-call service registration
- **Comprehensive XML documentation** on all public types, methods, and properties for IntelliSense support
- **Immutable built configs** — all builders snapshot collections at build time for thread safety
- **SourceLink enabled** — step through NuGet package source in your debugger
- **Deterministic builds** with symbol packages (`.snupkg`) for source-level debugging

## Installation

The SDK is not yet published to NuGet. For now, reference the project directly:

```csharp
// In your project file
<ProjectReference Include="path/to/Squad.SDK.NET/Squad.SDK.NET.csproj" />
```

## Quick Start

### 1. Configure Dependency Injection

```csharp
var services = new ServiceCollection();

services.AddSquadSdk(builder =>
{
    builder
        .WithTeam(team => team.Named("dev-squad"))
        .WithAgent(agent =>
        {
            agent
                .Named("architect")
                .WithCharter("path/to/architect/charter.md");
        })
        .WithRouting(routing =>
        {
            routing
                .ForWorkType("design-review")
                .RouteTo(["architect"])
                .WithTier(ResponseTier.Full);
        });
});

var provider = services.BuildServiceProvider();
```

### 2. Start the Client

```csharp
var client = provider.GetRequiredService<ISquadClient>();
await client.StartAsync();
```

### 3. Create a Session

```csharp
var session = await client.CreateSessionAsync(new SquadSessionConfig
{
    ClientName = "MyApp",
    Model = "gpt-5"
});
```

### 4. Send a Message

```csharp
var options = new SquadMessageOptions
{
    Prompt = "Design a caching strategy for high-traffic endpoints"
};

var response = await session.SendAsync(options);
```

### 5. Subscribe to Events

```csharp
session.On(evt =>
{
    if (evt.Type == SquadEventType.SessionMessage)
    {
        Console.WriteLine($"Response: {evt.Payload}");
    }
    else if (evt.Type == SquadEventType.Usage)
    {
        var usage = (UsagePayload)evt.Payload;
        Console.WriteLine($"Tokens: {usage.InputTokens + usage.OutputTokens}");
    }
});

await Task.Delay(Timeout.Infinite);
```

## Fluent Builder API

The `SquadBuilder` class exposes a chainable API for configuring your squad:

```csharp
SquadBuilder.Create()
    .WithTeam(team => 
    {
        team.Named("platform-team")
           .WithDescription("Backend infrastructure specialists");
    })
    .WithAgent(agent =>
    {
        agent
            .Named("db-expert")
            .WithCharter("./charters/db.md")
            .WithAllowedTools(["sql_query", "analyze_schema"])
            .WithModelPreference("gpt-5");
    })
    .WithAgent(agent =>
    {
        agent
            .Named("api-designer")
            .WithCharter("./charters/api.md");
    })
    .WithRouting(routing =>
    {
        routing
            .ForWorkType("database-optimization")
            .RouteTo(["db-expert"])
            .WithTier(ResponseTier.Full)
            .WithPriority(10);

        routing
            .ForWorkType("api-design")
            .RouteTo(["api-designer"])
            .WithTier(ResponseTier.Standard)
            .WithPriority(5);

        routing
            .SetFallbackBehavior(RoutingFallbackBehavior.Coordinator);
    })
    .WithModels(models =>
    {
        models.Prefer("gpt-5");
    })
    .WithHooks(new PolicyConfig
    {
        AllowedWritePaths = ["/var/app/data", "/var/app/logs"],
        BlockedCommands = ["rm -rf", "chmod 000"],
        MaxAskUserPerSession = 5
    })
    .Build();
```

## Key Concepts

### Agent Charters

Agents are defined by **charters** — markdown files with optional YAML frontmatter:

```markdown
---
name: architect
displayName: Solution Architect
role: technical-lead
expertise: [system-design, performance, scalability]
style: detail-oriented, principled
allowedTools: [code_search, architecture_tool]
modelPreference: gpt-5
---

You are a solution architect specializing in high-performance systems.
Design solutions that scale to millions of users...
```

Charters are compiled via `CharterCompiler.CompileAsync()` and parsed into `AgentCharter` objects that define the agent's identity, tools, and behavioral instructions.

### Coordinator & Routing

The **Coordinator** matches incoming messages against routing rules and dispatches to appropriate agents. Routing decisions include:

- **ResponseTier** — how deeply to think:
  - `Direct`: Quick, immediate response (no reasoning)
  - `Lightweight`: Simple analysis (fast)
  - `Standard`: Balanced analysis (default)
  - `Full`: Deep reasoning and multiple perspectives
- **Agents**: Which agents to involve
- **Parallel**: Whether agents work concurrently (fan-out) or sequentially

Rules are matched by work-type keywords; unmatched messages fall back to coordinator routing (all active agents in parallel) or a designated default agent.

### Event Bus

The `EventBus` provides pub/sub event dispatch via `Channel<T>`:

```csharp
eventBus.Subscribe(SquadEventType.SessionMessage, async evt =>
{
    Console.WriteLine($"Message from agent: {evt.Payload}");
});

eventBus.SubscribeAll(async evt =>
{
    // Handle all events
});
```

Event types include: `SessionCreated`, `SessionMessage`, `MessageDelta`, `SessionToolCall`, `Usage`, `ReasoningDelta`, `CoordinatorRouting`, `SessionError`, and `SessionDestroyed`.

### Hook Pipeline

The `HookPipeline` intercepts tool calls before and after execution for governance:

```csharp
// Pre-tool hooks (allow, block, or modify arguments)
hookPipeline.AddPreToolHook(async context =>
{
    if (context.ToolName == "delete_file" && context.Arguments["path"].Contains("/system"))
        return PreToolUseResult.Block("System files cannot be deleted");
    return PreToolUseResult.Allow();
});

// Post-tool hooks (validate outcomes)
hookPipeline.AddPostToolHook(async context =>
{
    if (!context.ExecutionSucceeded)
        return PostToolUseResult.Error("Tool execution failed");
    return PostToolUseResult.Ok();
});
```

Built-in policies: `AllowedWritePaths`, `BlockedCommands`, `MaxAskUserPerSession`.

### Cost Tracking

The `CostTracker` aggregates usage across sessions and models:

```csharp
var tracker = new CostTracker();
tracker.RecordUsage("gpt-5", sessionId, inputTokens: 1000, outputTokens: 500);

var summary = tracker.GetTotalSummary();
Console.WriteLine($"Total Cost: ${summary.TotalEstimatedCost}");

// Per-model breakdown
foreach (var (model, usage) in summary.ByModel)
{
    Console.WriteLine($"{model}: {usage.TotalInputTokens} input tokens");
}
```

### Session Pool

**AgentSessionManager** maintains active sessions for each agent, preventing session leaks and enabling efficient resource reuse. Sessions are created on-demand and cached for the agent's lifetime.

## Dependency Injection

Register the full SDK stack with a single call:

```csharp
var services = new ServiceCollection();

services.AddSquadSdk(builder =>
{
    builder
        .WithTeam(team => team.Named("my-squad"))
        .WithAgent(agent => agent.Named("agent1").WithCharter("./charter.md"));
});

var provider = services.BuildServiceProvider();

var client = provider.GetRequiredService<ISquadClient>();
var coordinator = provider.GetRequiredService<ICoordinator>();
var eventBus = provider.GetRequiredService<IEventBus>();
```

Registered services:
- `ISquadClient` — session lifecycle (create, resume, delete, list)
- `IEventBus` — pub/sub event dispatch
- `IHookPipeline` — tool governance (pre/post hooks)
- `ICoordinator` — routing and message dispatch
- `IAgentSessionManager` — session pooling
- `SquadConfig` — configuration (if provided to builder)

## Architecture

```
┌──────────────────────────────────────────┐
│        SquadClient (ISquadClient)        │
│  • Session lifecycle (create/resume)     │
│  • Wraps GitHub.Copilot.SDK              │
└─────────────────┬────────────────────────┘
                  │
          ┌───────▼────────┐
          │  SquadSession   │
          │ (ISquadSession) │
          └───────┬─────────┘
                  │
    ┌─────────────┼─────────────┐
    │             │             │
    ▼             ▼             ▼
┌─────────┐ ┌───────────┐ ┌──────────────┐
│ EventBus│ │Coordinator│ │ HookPipeline │
│(pub/sub)│ │ (routing) │ │ (governance) │
└─────────┘ └─────┬─────┘ └──────────────┘
                  │
        ┌─────────▼──────────────┐
        │  AgentSessionManager   │
        │  • Session pooling     │
        │  • Per-agent lifecycle │
        └────────────────────────┘

┌────────────────┐ ┌────────────────┐ ┌─────────────────┐
│ CharterCompiler│ │  CostTracker   │ │  SkillRegistry  │
│ (MD+YAML→obj)  │ │ (usage/costs)  │ │  (tool loading) │
└────────────────┘ └────────────────┘ └─────────────────┘

┌────────────────┐ ┌────────────────┐ ┌─────────────────┐
│  SquadBuilder  │ │ Import/Export  │ │ PlatformDetector│
│ (fluent config)│ │ (sharing JSON) │ │  (OS/IDE/term)  │
└────────────────┘ └────────────────┘ └─────────────────┘

┌──────────────────────────────────────────┐
│         SkillSecurityScanner             │
│  • Static analysis of skill markdown     │
│  • Detects credentials, exec patterns,  │
│    cred file reads, priv escalation      │
└──────────────────────────────────────────┘
```

## AOT Readiness

Squad.SDK.NET is fully compatible with .NET Native AOT publishing:

- **`IsAotCompatible` is set to `true`** in the project file — the compiler enforces AOT safety
- **All JSON serialization** uses source-generated contexts:
  - `SquadStateJsonContext` — squad state persistence
  - `SharingJsonContext` — import/export of squad configurations
  - `ConfigJsonContext` — configuration file loading
  - `RemoteJsonContext` — remote bridge protocol messages
- **Zero reflection** — no `Activator.CreateInstance`, no `Type.GetType()`, no dynamic code generation anywhere in the SDK
- **All DI registrations** use concrete factory delegates (no open-generic or reflection-based resolution)
- Fully compatible with `dotnet publish -r <rid> /p:PublishAot=true`

## Testing

The SDK includes a comprehensive test suite with 20+ test classes and 582+ test cases (and growing):

```
Squad.SDK.NET.Tests/
├── AdvancedModulesTests.cs           — Advanced module integration
├── AgentSessionManagerTests.cs       — Session pooling
├── BuiltInToolsTests.cs              — Built-in tool execution
├── CharterCompilerTests.cs           — Charter parsing
├── ConfigAndBuilderParityTests.cs    — Config/builder equivalence
├── ConfigLoaderTests.cs              — AOT config loading and serialization
├── ConfigValidationTests.cs          — Configuration validation
├── CoordinatorTests.cs               — Routing and dispatch logic
├── CostTrackerTests.cs               — Usage aggregation
├── DirectResponseTests.cs            — Direct response tier
├── EventBusTests.cs                  — Pub/sub correctness
├── FanOutTests.cs                    — Parallel agent fan-out
├── HookPipelineTests.cs              — Tool governance policies
├── ServiceCollectionExtensionsTests.cs — DI registration
├── SessionPoolTests.cs               — Session pool lifecycle
├── SkillRegistryTests.cs             — Skill loading and lookup
├── SkillSecurityScannerTests.cs      — Skill security scanning patterns
├── SquadBuilderTests.cs              — Fluent builder validation
├── SquadClientIdempotencyTests.cs    — Client idempotency
├── StorageStateResolutionTests.cs    — Storage state resolution
└── SubAgentTests.cs                  — Sub-agent orchestration
```

Run tests with:
```bash
dotnet test
```

## Requirements

- **.NET 10** or later (AOT-compatible)
- **GitHub.Copilot.SDK** v0.2.1 or later
- **Microsoft.Extensions.AI.Abstractions** v10.4.1 or later
- **Microsoft.Extensions.DependencyInjection.Abstractions** v10.0.5 or later
- **Microsoft.Extensions.Logging.Abstractions** v10.0.5 or later

All dependencies are AOT-safe and trimming-compatible.

## Documentation

For detailed usage examples covering all SDK features, see the **[Usage Examples Guide](../../docs/examples.md)**.

## License

See the root `LICENSE` file for licensing details.
