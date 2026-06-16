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
- **Skill security scanner** — static analysis of skill markdown for embedded credentials, download-execute patterns, and privilege escalation
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

Reference the project directly during local development:

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
        .WithTeam(team => team.Name("dev-squad"))
        .WithAgent(agent =>
        {
            agent
                .Name("architect")
                .Charter("path/to/architect/charter.md")
                .Role("technical-lead");
        })
        .WithRouting(routing =>
        {
            routing
                .AddRule("design-review", ["architect"], tier: ResponseTier.Full)
                .DefaultAgent("architect");
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

var response = await session.SendAndWaitAsync(options);
```

### 5. Subscribe to Events

```csharp
var eventBus = provider.GetRequiredService<IEventBus>();

eventBus.Subscribe(SquadEventType.SessionMessage, async evt =>
{
    Console.WriteLine($"Response: {evt.Payload}");
});

eventBus.Subscribe(SquadEventType.Usage, async evt =>
{
    if (evt.Payload is UsagePayload usage)
        Console.WriteLine($"Tokens: {usage.InputTokens + usage.OutputTokens}");
});
```

## Fluent Builder API

The `SquadBuilder` class exposes a chainable API for configuring your squad:

```csharp
SquadBuilder.Create()
    .WithTeam(team => 
    {
        team.Name("platform-team")
            .Description("Backend infrastructure specialists");
    })
    .WithAgent(agent =>
    {
        agent
            .Name("db-expert")
            .Charter("./charters/db.md")
            .Role("database")
            .AllowTools("sql_query", "analyze_schema")
            .Model("gpt-5");
    })
    .WithAgent(agent =>
    {
        agent
            .Name("api-designer")
            .Charter("./charters/api.md")
            .Role("api");
    })
    .WithRouting(routing =>
    {
        routing
            .AddRule("database-optimization", ["db-expert"], tier: ResponseTier.Full, priority: 10)
            .AddRule("api-design", ["api-designer"], tier: ResponseTier.Standard, priority: 5)
            .Fallback(RoutingFallbackBehavior.Coordinator);
    })
    .WithModels(models =>
    {
        models.Default("gpt-5");
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
