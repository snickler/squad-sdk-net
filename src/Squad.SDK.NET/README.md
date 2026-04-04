# Squad.SDK.NET

Multi-agent orchestration SDK for .NET 10, wrapping GitHub.Copilot.SDK with fluent configuration and advanced routing.

## Overview

Squad.SDK.NET is a .NET port of [@bradygaster/squad-sdk](https://github.com/bradygaster/squad-sdk), designed to orchestrate teams of AI agents using the GitHub Copilot SDK. It provides a fluent builder API for defining agent charters, routing rules, and governance policies — with built-in support for session pooling, event pub/sub, cost tracking, and tool access control.

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
┌──────────────────────────────────────┐
│      SquadClient (ISquadClient)      │
│  • Session lifecycle (create/resume) │
│  • Wraps GitHub.Copilot.SDK          │
└─────────────────┬────────────────────┘
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
```

## Testing

The SDK includes a comprehensive test suite (14 test classes, 133 test cases):

```
Squad.SDK.NET.Tests/
├── SquadBuilderTests.cs           — Fluent builder validation
├── CoordinatorTests.cs            — Routing and dispatch logic
├── HookPipelineTests.cs           — Tool governance policies
├── EventBusTests.cs               — Pub/sub correctness
├── CostTrackerTests.cs            — Usage aggregation
├── CharterCompilerTests.cs        — Charter parsing
├── AgentSessionManagerTests.cs    — Session pooling
├── ServiceCollectionExtensionsTests.cs
├── SkillRegistryTests.cs
├── SessionPoolTests.cs
├── ConfigValidationTests.cs
├── DirectResponseTests.cs
├── FanOutTests.cs
└── BuiltInToolsTests.cs
```

Run tests with:
```bash
dotnet test Squad.SDK.NET.Tests
```

## Requirements

- **.NET 10** or later
- **GitHub.Copilot.SDK** v0.2.1-preview.1 or later
- **Microsoft.Extensions.AI.Abstractions** (for extensible model interface)
- **Microsoft.Extensions.DependencyInjection.Abstractions** (for service registration)

## License

See the root `LICENSE` file for licensing details.
