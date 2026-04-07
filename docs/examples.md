# Squad.SDK.NET — Usage Examples

A cookbook of copy-paste-ready examples for every major feature of the Squad SDK.

> **Target framework:** .NET 10 · **AOT-safe** — all serialization uses `JsonSerializerContext` source generators.

---

## Table of Contents

1. [Getting Started](#1-getting-started)
2. [Builder API Deep Dive](#2-builder-api-deep-dive)
3. [Agent Charters](#3-agent-charters)
4. [Coordinator & Routing](#4-coordinator--routing)
5. [Event System](#5-event-system)
6. [Hook Pipeline (Governance)](#6-hook-pipeline-governance)
7. [Cost Tracking](#7-cost-tracking)
8. [Session Management](#8-session-management)
9. [Configuration](#9-configuration)
10. [Skills System](#10-skills-system)
11. [Import/Export (Sharing)](#11-importexport-sharing)
12. [Storage Providers](#12-storage-providers)
13. [Platform Detection](#13-platform-detection)
14. [Multi-Squad Management](#14-multi-squad-management)
15. [Casting Engine](#15-casting-engine)
16. [Advanced Patterns](#16-advanced-patterns)

---

## 1. Getting Started

### Installing the SDK

Add a project reference (or, once published, a NuGet package reference):

```xml
<ItemGroup>
  <ProjectReference Include="..\Squad.SDK.NET\Squad.SDK.NET.csproj" />
</ItemGroup>
```

### Minimal Setup with Dependency Injection

The `AddSquadSdk` extension method registers all core services in one call:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Extensions;

var services = new ServiceCollection();

services.AddLogging(logging => logging.AddConsole());
services.AddSquadSdk(builder =>
{
    builder.WithTeam(team =>
    {
        team.Name("my-team")
            .Description("A minimal squad");
    });
});

var provider = services.BuildServiceProvider();
```

### Creating Your First Client and Session

```csharp
using Squad.SDK.NET.Abstractions;

var client = provider.GetRequiredService<ISquadClient>();

await client.StartAsync();

// Create a session with default settings
var session = await client.CreateSessionAsync();

// Send a message and wait for the response
var response = await session.SendAndWaitAsync(
    new SquadMessageOptions { Prompt = "Hello, Squad!" });

Console.WriteLine(response);

// Clean up
await session.DisposeAsync();
await client.StopAsync();
```

### Creating a Session with Custom Configuration

```csharp
var config = new SquadSessionConfig
{
    ClientName = "my-agent",
    Model = "claude-sonnet-4.6",
    ReasoningEffort = "high",
    SystemMessage = "You are a helpful coding assistant.",
    AvailableTools = ["read_file", "write_file", "bash"],
    ExcludedTools = ["dangerous_tool"]
};

var session = await client.CreateSessionAsync(config);
```

---

## 2. Builder API Deep Dive

The `SquadBuilder` is the fluent entry point for building a complete `SquadConfig`.

> **Immutability guarantee:** All builders snapshot their internal collections at `Build()` time. You can safely reuse a builder after calling `Build()` — previously built configs will not be affected by subsequent mutations.

### Team Configuration

```csharp
using Squad.SDK.NET.Builder;
using Squad.SDK.NET.Config;

var config = SquadBuilder.Create()
    .WithTeam(team =>
    {
        team.Name("acme-dev-squad")
            .Description("Full-stack development team")
            .DefaultModel("gpt-5")
            .DefaultTier(ModelTier.Standard);
    })
    .Build();
```

### Agent Configuration

```csharp
var config = SquadBuilder.Create()
    .WithTeam(team => team.Name("my-team"))
    .WithAgent(agent =>
    {
        agent.Name("backend-dev")
              .DisplayName("Backend Developer")
              .Role("backend")
              .Expertise("C#", ".NET", "SQL", "REST APIs")
              .Style("concise and precise")
              .Prompt("You are a senior .NET backend developer.")
              .Model("claude-sonnet-4.6")
              .AllowTools("read_file", "write_file", "bash")
              .ExcludeTools("browser")
              .Status(AgentStatus.Active)
              .Charter("# Backend Dev Charter\nOwns API layer.")
              .Capabilities(
                  new AgentCapability { Name = "code-review", Enabled = true },
                  new AgentCapability { Name = "deploy", Description = "Can trigger deploys" })
              .Budget(b => b.PerAgentSpawn(0.50m).PerSession(2.00m));
    })
    .Build();
```

### Routing Configuration

```csharp
using Squad.SDK.NET.Coordinator;

var config = SquadBuilder.Create()
    .WithTeam(team => team.Name("my-team"))
    .WithAgent(agent => agent.Name("frontend").Role("frontend"))
    .WithAgent(agent => agent.Name("backend").Role("backend"))
    .WithAgent(agent => agent.Name("tester").Role("tester"))
    .WithRouting(routing =>
    {
        routing
            // Work types map to agent(s) with a tier and priority
            .AddRule(
                workType: WorkType.FeatureDev,
                agents: ["frontend", "backend"],
                tier: ResponseTier.Standard,
                priority: 10)
            .AddRule(
                workType: WorkType.Testing,
                agents: ["tester"],
                tier: ResponseTier.Lightweight,
                priority: 5)
            .AddRule(
                workType: WorkType.Architecture,
                agents: ["backend"],
                tier: ResponseTier.Full,
                priority: 20)
            // Where to route when no rule matches
            .DefaultAgent("backend")
            .Fallback(RoutingFallbackBehavior.DefaultAgent);
    })
    .Build();
```

### Model Preferences

```csharp
var config = SquadBuilder.Create()
    .WithTeam(team => team.Name("my-team"))
    .WithModels(models =>
    {
        models.Default("gpt-5")
              .DefaultTier(ModelTier.Standard)
              .FallbackChain(ModelTier.Premium, "claude-opus-4.6", "gpt-5")
              .FallbackChain(ModelTier.Standard, "claude-sonnet-4.6", "gpt-5")
              .FallbackChain(ModelTier.Fast, "claude-haiku-4.5", "gpt-5-mini")
              .PreferSameProvider();
    })
    .Build();
```

### Hook Policies

Two ways to configure hooks — via `PolicyConfig` or via `HooksBuilder`:

```csharp
using Squad.SDK.NET.Hooks;

// Option 1: PolicyConfig record
var config = SquadBuilder.Create()
    .WithTeam(team => team.Name("my-team"))
    .WithHooks(new PolicyConfig
    {
        AllowedWritePaths = ["src/", "tests/"],
        BlockedCommands = ["rm -rf", "sudo"],
        MaxAskUserPerSession = 3,
        ScrubPii = true,
        ReviewerLockout = true
    })
    .Build();

// Option 2: HooksBuilder (fluent)
var config2 = SquadBuilder.Create()
    .WithTeam(team => team.Name("my-team"))
    .WithHooks(hooks =>
    {
        hooks.AllowedWritePaths("src/", "tests/", "docs/")
             .BlockedCommands("rm -rf", "format c:")
             .MaxAskUser(5)
             .ScrubPii()
             .ReviewerLockout();
    })
    .Build();
```

### Budget Configuration

```csharp
var config = SquadBuilder.Create()
    .WithTeam(team => team.Name("my-team"))
    .WithBudget(budget =>
    {
        budget.PerAgentSpawn(0.25m)
              .PerSession(5.00m)
              .WarnAt(4.00m);
    })
    .Build();
```

### Telemetry Configuration

```csharp
var config = SquadBuilder.Create()
    .WithTeam(team => team.Name("my-team"))
    .WithTelemetry(telemetry =>
    {
        telemetry.Enabled(true)
                 .ServiceName("squad-api")
                 .Endpoint("https://otel.example.com:4317")
                 .SampleRate(0.5)
                 .AspireDefaults();
    })
    .Build();
```

### Casting Configuration

```csharp
var config = SquadBuilder.Create()
    .WithTeam(team => team.Name("my-team"))
    .WithCasting(casting =>
    {
        casting.AllowlistUniverses("marvel", "star-wars", "lotr")
               .OverflowStrategy(OverflowStrategy.Rotate)
               .Capacity(10);
    })
    .Build();
```

### Ceremony Configuration

```csharp
var config = SquadBuilder.Create()
    .WithTeam(team => team.Name("my-team"))
    .WithCeremony(ceremony =>
    {
        ceremony.Name("standup")
                .Trigger("daily")
                .Schedule("0 9 * * MON-FRI")
                .Participants("frontend", "backend", "tester")
                .Agenda(
                    "What did you accomplish yesterday?",
                    "What will you work on today?",
                    "Any blockers?")
                .Hooks(new PolicyConfig { MaxAskUserPerSession = 1 });
    })
    .Build();
```

### Defaults Configuration

```csharp
var config = SquadBuilder.Create()
    .WithTeam(team => team.Name("my-team"))
    .WithDefaults(defaults =>
    {
        defaults.Model("gpt-5")
                .Model(new ModelPreference
                {
                    Preferred = "claude-sonnet-4.6",
                    Rationale = "Best balance of speed and quality",
                    Fallback = "gpt-5"
                })
                .Budget(b => b.PerSession(3.00m).WarnAt(2.50m));
    })
    .Build();
```

### Skill Registration via Builder

```csharp
var config = SquadBuilder.Create()
    .WithTeam(team => team.Name("my-team"))
    .WithSkill(skill =>
    {
        skill.Name("dotnet-testing")
             .Description("Generates xUnit tests for .NET code")
             .Domain("testing")
             .Confidence(SkillConfidenceLevel.High)
             .Source("built-in")
             .Content("When writing tests, use xUnit with FluentAssertions.")
             .Tools("read_file", "write_file", "bash");
    })
    .Build();
```

### Full Builder Example

```csharp
var config = SquadBuilder.Create()
    .WithTeam(team =>
    {
        team.Name("product-squad")
            .Description("End-to-end product development")
            .DefaultModel("gpt-5")
            .DefaultTier(ModelTier.Standard);
    })
    .WithAgent(agent =>
    {
        agent.Name("lead")
             .Role("lead")
             .Expertise("architecture", "code review", "mentoring")
             .Model("claude-opus-4.6")
             .Prompt("You are the technical lead.");
    })
    .WithAgent(agent =>
    {
        agent.Name("dev")
             .Role("backend")
             .Expertise("C#", ".NET", "EF Core")
             .AllowTools("read_file", "write_file", "bash")
             .Prompt("You are a backend developer.");
    })
    .WithAgent(agent =>
    {
        agent.Name("qa")
             .Role("tester")
             .Expertise("xUnit", "integration testing")
             .Prompt("You write comprehensive tests.");
    })
    .WithRouting(routing =>
    {
        routing.AddRule(WorkType.FeatureDev, ["lead", "dev"], ResponseTier.Standard, priority: 10)
               .AddRule(WorkType.Testing, ["qa"], ResponseTier.Lightweight, priority: 5)
               .AddRule(WorkType.Architecture, ["lead"], ResponseTier.Full, priority: 20)
               .DefaultAgent("lead")
               .Fallback(RoutingFallbackBehavior.DefaultAgent);
    })
    .WithModels(models =>
    {
        models.Default("gpt-5")
              .FallbackChain(ModelTier.Premium, "claude-opus-4.6")
              .FallbackChain(ModelTier.Fast, "gpt-5-mini", "claude-haiku-4.5");
    })
    .WithHooks(hooks =>
    {
        hooks.AllowedWritePaths("src/", "tests/")
             .BlockedCommands("rm -rf")
             .ScrubPii()
             .ReviewerLockout();
    })
    .WithBudget(budget =>
    {
        budget.PerSession(10.00m).WarnAt(8.00m);
    })
    .WithTelemetry(telemetry =>
    {
        telemetry.Enabled(true).ServiceName("product-squad");
    })
    .WithCasting(casting =>
    {
        casting.AllowlistUniverses("default").Capacity(5);
    })
    .WithDefaults(defaults =>
    {
        defaults.Model("gpt-5")
                .Budget(b => b.PerAgentSpawn(0.50m));
    })
    .WithSkill(skill =>
    {
        skill.Name("code-review")
             .Description("Automated code review")
             .Confidence(SkillConfidenceLevel.High);
    })
    .WithCeremony(ceremony =>
    {
        ceremony.Name("retro")
                .Trigger("sprint-end")
                .Participants("lead", "dev", "qa")
                .Agenda("What went well?", "What to improve?");
    })
    .Build();
```

---

## 3. Agent Charters

### Charter Markdown Format

Charters use YAML frontmatter followed by a markdown prompt body. Save as `charter.md`:

```markdown
---
name: backend-dev
displayName: Backend Developer
role: backend
expertise: [C#, .NET, SQL]
style: concise
modelPreference: claude-sonnet-4.6
allowedTools: [read_file, write_file, bash]
excludedTools: [browser]
---

You are a senior backend developer specializing in .NET APIs.

## What I Own
- REST API endpoints
- Database migrations
- Service layer logic

## Standards
- Follow Clean Architecture
- All public methods must have XML doc comments
```

### Compiling Charters

```csharp
using Squad.SDK.NET.Agents;

// Compile a single charter file
AgentCharter charter = await CharterCompiler.CompileAsync("agents/backend/charter.md");

Console.WriteLine($"Agent: {charter.Name}");
Console.WriteLine($"Role: {charter.Role}");
Console.WriteLine($"Model: {charter.ModelPreference}");
Console.WriteLine($"Tools: {string.Join(", ", charter.AllowedTools ?? [])}");

// Compile all charter.md files under a team root
IReadOnlyList<AgentCharter> allCharters =
    await CharterCompiler.CompileAllAsync(".squad/agents");

foreach (var c in allCharters)
{
    Console.WriteLine($"  {c.Name} ({c.Role})");
}
```

### Building an AgentCharter Programmatically

```csharp
var charter = new AgentCharter
{
    Name = "reviewer",
    Role = "code-review",
    Expertise = ["security", "performance", "clean code"],
    Prompt = "You review pull requests for quality and security issues.",
    ModelPreference = "claude-sonnet-4.6",
    AllowedTools = ["read_file", "grep"],
    ExcludedTools = ["write_file", "bash"]
};
```

---

## 4. Coordinator & Routing

### How Routing Decisions Work

The `Coordinator` matches incoming messages against routing rules by work-type keywords, then selects agents and a response tier.

```csharp
using Squad.SDK.NET.Coordinator;
using Squad.SDK.NET.Abstractions;

var coordinator = provider.GetRequiredService<ICoordinator>();
await coordinator.InitializeAsync();

// Route a message — returns which agents should handle it
RoutingDecision decision = await coordinator.RouteAsync(
    "Fix the login bug in the authentication module");

Console.WriteLine($"Tier: {decision.Tier}");           // e.g., Standard
Console.WriteLine($"Agents: {string.Join(", ", decision.Agents)}");
Console.WriteLine($"Parallel: {decision.Parallel}");
Console.WriteLine($"Rationale: {decision.Rationale}");
```

### Response Tiers

| Tier | When to Use |
|------|-------------|
| `ResponseTier.Direct` | Simple greetings or FAQ — no agent needed |
| `ResponseTier.Lightweight` | Quick lookups, short answers |
| `ResponseTier.Standard` | Normal feature work, bug fixes |
| `ResponseTier.Full` | Architecture reviews, complex design tasks |

### Direct Response Handling

For trivial messages, `DirectResponse` can reply without routing to any agent:

```csharp
if (DirectResponse.TryGetDirectResponse("hello", out var reply))
{
    Console.WriteLine(reply);
    // "Hello! I'm Squad, your AI development team. How can I help you today?"
}
```

### Executing a Routing Decision

```csharp
// Route then execute
var decision = await coordinator.RouteAsync("Build the user registration feature");
await coordinator.ExecuteAsync(decision, "Build the user registration feature");
```

### Fan-Out for Parallel Agent Work

```csharp
using Squad.SDK.NET.Coordinator;

var agentManager = provider.GetRequiredService<IAgentSessionManager>();

// Define multiple agent charters
var charters = new List<AgentCharter>
{
    new AgentCharter
    {
        Name = "frontend-worker",
        Role = "frontend",
        Prompt = "Build the React component."
    },
    new AgentCharter
    {
        Name = "backend-worker",
        Role = "backend",
        Prompt = "Build the API endpoint."
    }
};

// Spawn all agents in parallel and send them the same message
IReadOnlyList<SquadEvent> results = await FanOut.SpawnParallelAsync(
    agentManager,
    charters,
    message: "Implement user profile editing",
    mode: ResponseTier.Standard);

foreach (var evt in results)
{
    Console.WriteLine($"[{evt.Type}] {evt.AgentName}: {evt.Payload}");
}
```

### Sub-Agent Fan-Out

```csharp
// Spawn sub-agents under a parent agent
IReadOnlyList<SquadEvent> subResults = await FanOut.SpawnSubAgentsParallelAsync(
    agentManager,
    parentAgentName: "lead",
    charters,
    message: "Investigate performance bottlenecks",
    mode: ResponseTier.Lightweight);
```

---

## 5. Event System

### Subscribing to Specific Event Types

```csharp
using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Events;

var eventBus = provider.GetRequiredService<IEventBus>();

// Subscribe to session errors
IDisposable sub = eventBus.Subscribe(SquadEventType.SessionError, async evt =>
{
    if (evt.Payload is SessionErrorPayload error)
    {
        Console.WriteLine($"ERROR in session {evt.SessionId}: {error.Message}");
    }
});

// Subscribe to usage events for cost monitoring
eventBus.Subscribe(SquadEventType.Usage, async evt =>
{
    if (evt.Payload is UsagePayload usage)
    {
        Console.WriteLine($"Model: {usage.Model}, " +
                          $"In: {usage.InputTokens}, Out: {usage.OutputTokens}, " +
                          $"Cost: ${usage.EstimatedCost:F4}");
    }
});
```

### Subscribing to All Events

```csharp
IDisposable allSub = eventBus.SubscribeAll(async evt =>
{
    Console.WriteLine($"[{evt.Timestamp:HH:mm:ss}] {evt.Type} " +
                      $"agent={evt.AgentName} session={evt.SessionId}");
});
```

### Event Types and Payloads

| Event Type | Payload Type | Description |
|---|---|---|
| `SessionCreated` | — | A new session was created |
| `SessionIdle` | — | Session finished processing |
| `SessionError` | `SessionErrorPayload` | An error occurred |
| `SessionDestroyed` | — | Session was deleted |
| `SessionMessage` | `string` (content) | Assistant message received |
| `SessionToolCall` | `ToolCallPayload` | Tool execution started/completed |
| `AgentMilestone` | `AgentState` or `SubAgentSpawnedPayload` | Agent lifecycle change |
| `CoordinatorRouting` | `RoutingDecision` | A routing decision was made |
| `MessageDelta` | `StreamDeltaPayload` | Streaming content chunk |
| `Usage` | `UsagePayload` | Token usage report |
| `ReasoningDelta` | `ReasoningDeltaPayload` | Reasoning content chunk |

### Streaming Deltas

```csharp
eventBus.Subscribe(SquadEventType.MessageDelta, async evt =>
{
    if (evt.Payload is StreamDeltaPayload delta)
    {
        Console.Write(delta.Content); // Print without newline for streaming
    }
});
```

### Unsubscribing (Disposal Pattern)

```csharp
IDisposable subscription = eventBus.Subscribe(
    SquadEventType.SessionMessage, async evt => { /* ... */ });

// Later, unsubscribe:
subscription.Dispose();
```

### Emitting Custom Events

```csharp
await eventBus.EmitAsync(new SquadEvent
{
    Type = SquadEventType.AgentMilestone,
    AgentName = "backend-dev",
    SessionId = "session-123",
    Payload = AgentState.Active
});
```

---

## 6. Hook Pipeline (Governance)

### Pre-Tool-Use Hooks

Pre-tool hooks run **before** a tool executes. They can allow, block, or modify the call.

```csharp
using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Hooks;

var pipeline = provider.GetRequiredService<IHookPipeline>();

// Custom pre-hook: block all bash commands containing "curl"
pipeline.AddPreToolHook(context =>
{
    if (context.ToolName == "bash"
        && context.Arguments.TryGetValue("command", out var cmd)
        && cmd is string command
        && command.Contains("curl", StringComparison.OrdinalIgnoreCase))
    {
        return Task.FromResult(
            PreToolUseResult.Block("Network access via curl is not allowed."));
    }

    return Task.FromResult(PreToolUseResult.Allow());
});
```

### Modifying Tool Arguments

```csharp
// Pre-hook that appends --dry-run to all bash commands
pipeline.AddPreToolHook(context =>
{
    if (context.ToolName == "bash"
        && context.Arguments.TryGetValue("command", out var cmd)
        && cmd is string command)
    {
        var modified = new Dictionary<string, object?>(context.Arguments)
        {
            ["command"] = command + " --dry-run"
        };

        return Task.FromResult(PreToolUseResult.Modify(modified));
    }

    return Task.FromResult(PreToolUseResult.Allow());
});
```

### Post-Tool-Use Hooks

Post-tool hooks run **after** a tool completes. They can validate or transform results.

```csharp
pipeline.AddPostToolHook(context =>
{
    // Fail if the tool result contains sensitive patterns
    if (context.Result is string result
        && result.Contains("BEGIN RSA PRIVATE KEY"))
    {
        return Task.FromResult(
            PostToolUseResult.Fail("Tool output contains a private key — blocked."));
    }

    return Task.FromResult(PostToolUseResult.Ok());
});
```

### Running the Pipeline

```csharp
var preContext = new PreToolUseContext
{
    ToolName = "write_file",
    Arguments = new Dictionary<string, object?>
    {
        ["path"] = "src/Program.cs",
        ["content"] = "Console.WriteLine(\"hello\");"
    },
    AgentName = "backend-dev",
    SessionId = "session-abc"
};

PreToolUseResult preResult = await pipeline.RunPreToolHooksAsync(preContext);

switch (preResult.Action)
{
    case HookAction.Allow:
        Console.WriteLine("Tool call allowed.");
        break;
    case HookAction.Block:
        Console.WriteLine($"Blocked: {preResult.Reason}");
        break;
    case HookAction.Modify:
        Console.WriteLine("Arguments modified by hook.");
        // Use preResult.ModifiedArguments
        break;
}
```

### Built-in PII Scrubber Hook

```csharp
using Squad.SDK.NET.Hooks;

var scrubber = provider.GetRequiredService<PiiScrubberHook>();

// Register the hook
pipeline.AddPostToolHook(scrubber.CreateHook());

// Or use the static method directly
string cleaned = PiiScrubberHook.ScrubPii(
    "Contact john@example.com or call 555-123-4567, SSN 123-45-6789");
// Result: "Contact [EMAIL REDACTED] or call [PHONE REDACTED], SSN [SSN REDACTED]"
```

### Built-in Reviewer Lockout Hook

Prevents an agent from modifying artifacts it previously authored (conflict of interest):

```csharp
var lockout = provider.GetRequiredService<ReviewerLockoutHook>();

// Lock an agent out of a specific file
lockout.Lockout(artifactId: "src/Auth.cs", agentName: "backend-dev");

// Check lockout status
bool locked = lockout.IsLockedOut("src/Auth.cs", "backend-dev"); // true

// Register as a pre-tool hook
pipeline.AddPreToolHook(lockout.CreateHook());

// View all lockouts
IReadOnlyDictionary<string, string> lockedAgents = lockout.GetLockedAgents();

// Clear a specific lockout
lockout.ClearLockout("src/Auth.cs");

// Clear all lockouts
lockout.ClearAll();
```

### Policy-Driven HookPipeline

When a `PolicyConfig` is passed to the `HookPipeline` constructor, built-in enforcement hooks are automatically wired:

```csharp
var policy = new PolicyConfig
{
    AllowedWritePaths = ["src/", "tests/"],
    BlockedCommands = ["rm -rf", "sudo"],
    MaxAskUserPerSession = 3,
    ScrubPii = true,
    ReviewerLockout = true
};

var pipeline = new HookPipeline(policy);
// Built-in hooks for write-path enforcement, blocked commands,
// and ask_user limits are automatically active.
```

---

## 7. Cost Tracking

### Recording Usage

```csharp
using Squad.SDK.NET.Runtime;

var tracker = new CostTracker();

// Record token usage for a model + session
tracker.RecordUsage(
    model: Constants.Models.ClaudeSonnet,
    sessionId: "session-1",
    inputTokens: 1500,
    outputTokens: 800);

tracker.RecordUsage(
    model: Constants.Models.Gpt5Mini,
    sessionId: "session-2",
    inputTokens: 500,
    outputTokens: 200);
```

### Estimating Cost

```csharp
decimal cost = tracker.EstimateCost(
    model: Constants.Models.Gpt5,
    inputTokens: 10_000,
    outputTokens: 5_000);

Console.WriteLine($"Estimated cost: ${cost:F4}");
```

### Per-Model Breakdown

```csharp
ModelUsage sonnetUsage = tracker.GetModelUsage(Constants.Models.ClaudeSonnet);

Console.WriteLine($"Model: {sonnetUsage.Model}");
Console.WriteLine($"Requests: {sonnetUsage.RequestCount}");
Console.WriteLine($"Input tokens: {sonnetUsage.TotalInputTokens}");
Console.WriteLine($"Output tokens: {sonnetUsage.TotalOutputTokens}");
Console.WriteLine($"Cost: ${sonnetUsage.EstimatedCost:F4}");
```

### Per-Session Breakdown

```csharp
SessionUsage sessionUsage = tracker.GetSessionUsage("session-1");

Console.WriteLine($"Session: {sessionUsage.SessionId}");
Console.WriteLine($"Total tokens: {sessionUsage.TotalInputTokens + sessionUsage.TotalOutputTokens}");
Console.WriteLine($"Cost: ${sessionUsage.EstimatedCost:F4}");
```

### Total Summary

```csharp
CostSummary summary = tracker.GetTotalSummary();

Console.WriteLine($"Total input tokens: {summary.TotalInputTokens}");
Console.WriteLine($"Total output tokens: {summary.TotalOutputTokens}");
Console.WriteLine($"Total cost: ${summary.TotalEstimatedCost:F4}");
Console.WriteLine($"Models used: {summary.ByModel.Count}");
Console.WriteLine($"Sessions: {summary.BySession.Count}");

// Reset all tracking
tracker.Reset();
```

### Available Model Constants

```csharp
// Squad.SDK.NET.Runtime.Constants.Models
string gpt5       = Constants.Models.Gpt5;         // "gpt-5"
string gpt5Mini   = Constants.Models.Gpt5Mini;      // "gpt-5-mini"
string opus       = Constants.Models.ClaudeOpus;     // "claude-opus-4.6"
string sonnet     = Constants.Models.ClaudeSonnet;   // "claude-sonnet-4.6"
string haiku      = Constants.Models.ClaudeHaiku;    // "claude-haiku-4.5"
```

---

## 8. Session Management

### Creating Sessions with Configuration

```csharp
var client = provider.GetRequiredService<ISquadClient>();
await client.StartAsync();

// Minimal session
var session1 = await client.CreateSessionAsync();

// Configured session
var session2 = await client.CreateSessionAsync(new SquadSessionConfig
{
    SessionId = "custom-session-id",
    ClientName = "research-agent",
    Model = "claude-opus-4.6",
    ReasoningEffort = "high",
    SystemMessage = "You are a research analyst."
});
```

### Session Metadata

```csharp
// List all sessions
IReadOnlyList<SquadSessionMetadata> sessions = await client.ListSessionsAsync();

foreach (var meta in sessions)
{
    Console.WriteLine($"Session: {meta.SessionId}");
    Console.WriteLine($"  Agent: {meta.AgentName}");
    Console.WriteLine($"  Created: {meta.CreatedAt}");
    Console.WriteLine($"  Last Active: {meta.LastActiveAt}");
}
```

### Sending Messages with Attachments

```csharp
var options = new SquadMessageOptions
{
    Prompt = "Analyze this image for accessibility issues.",
    Attachments =
    [
        // File reference
        new SquadAttachment
        {
            Path = "screenshots/homepage.png",
            DisplayName = "Homepage Screenshot"
        },
        // Inline binary data
        new SquadAttachment
        {
            Data = Convert.ToBase64String(File.ReadAllBytes("logo.png")),
            MimeType = "image/png",
            DisplayName = "Logo"
        }
    ]
};

string? response = await session.SendAndWaitAsync(options, timeout: TimeSpan.FromMinutes(2));
```

### Resuming and Deleting Sessions

```csharp
// Resume a previous session
var resumed = await client.ResumeSessionAsync("session-id-from-before");

// Delete a session
await client.DeleteSessionAsync("old-session-id");
```

### Agent Session Manager

```csharp
var agentManager = provider.GetRequiredService<IAgentSessionManager>();

// Spawn an agent
var charter = new AgentCharter
{
    Name = "analyst",
    Role = "research",
    Prompt = "You perform deep research.",
    ModelPreference = "claude-opus-4.6"
};

AgentSessionInfo info = await agentManager.SpawnAsync(charter, ResponseTier.Full);
Console.WriteLine($"Agent state: {info.State}");        // Active
Console.WriteLine($"Session ID: {info.SessionId}");

// Get a specific agent
AgentSessionInfo? agent = agentManager.GetAgent("analyst");

// List all agents
IReadOnlyList<AgentSessionInfo> allAgents = agentManager.GetAllAgents();

// Resume an idle agent
await agentManager.ResumeAsync("analyst");

// Destroy an agent (cascades to sub-agents)
await agentManager.DestroyAsync("analyst");
```

### Sub-Agent Hierarchies

```csharp
// Spawn a sub-agent under a parent
var subCharter = new AgentCharter
{
    Name = "sub-researcher",
    Role = "research",
    Prompt = "You focus on database performance."
};

AgentSessionInfo subInfo = await agentManager.SpawnSubAgentAsync(
    parentAgentName: "analyst",
    charter: subCharter,
    mode: ResponseTier.Lightweight);

Console.WriteLine($"Depth: {subInfo.Depth}");                     // 1
Console.WriteLine($"Parent: {subInfo.ParentAgentName}");           // "analyst"

// Get all sub-agents of a parent
IReadOnlyList<AgentSessionInfo> subs = agentManager.GetSubAgents("analyst");

// Get the full agent tree
IReadOnlyList<AgentSessionInfo> tree = agentManager.GetAgentTree("analyst");
```

### Session Pooling

```csharp
using Squad.SDK.NET.Runtime;

var pool = new SessionPool();

// Add sessions to the pool
pool.Add(session1);
pool.Add(session2);

// Retrieve by ID
ISquadSession? s = pool.Get("session-id");

// List all pooled sessions
IReadOnlyList<ISquadSession> all = pool.GetAll();

// Remove a specific session
pool.Remove("session-id");

// Shut down all sessions
await pool.ShutdownAsync();
```

---

## 9. Configuration

### Loading from JSON Files

```csharp
using Squad.SDK.NET.Config;

// Async loading (preferred)
SquadConfig config = await ConfigLoader.LoadAsync("squad-config.json");

// Synchronous loading
SquadConfig configSync = ConfigLoader.LoadSync("squad-config.json");
```

### Validating Configuration

```csharp
IReadOnlyList<string> errors = ConfigLoader.Validate(config);

if (errors.Count > 0)
{
    Console.WriteLine("Configuration errors:");
    foreach (var error in errors)
    {
        Console.WriteLine($"  - {error}");
    }
}
else
{
    Console.WriteLine("Configuration is valid.");
}
```

### Example JSON Config File

```json
{
  "version": "1.0",
  "team": {
    "name": "my-squad",
    "description": "Development team",
    "defaultModel": "gpt-5",
    "defaultTier": "Standard"
  },
  "agents": [
    {
      "name": "dev",
      "role": "backend",
      "expertise": ["C#", ".NET"],
      "modelPreference": "claude-sonnet-4.6",
      "status": "Active"
    }
  ],
  "routing": {
    "rules": [
      {
        "workType": "feature-dev",
        "agents": ["dev"],
        "tier": "Standard",
        "priority": 10
      }
    ],
    "defaultAgent": "dev",
    "fallbackBehavior": "DefaultAgent"
  },
  "budget": {
    "perSession": 5.0,
    "warnAt": 4.0
  }
}
```

### AOT-Safe Serialization Contexts

Squad.SDK.NET ships with source-generated JSON contexts for AOT compatibility:

- `ConfigJsonContext` — for `SquadConfig` (used by `ConfigLoader`)
- `SharingJsonContext` — for `ExportedSquad`, `ImportResult`, and all config sub-types
- `SquadStateJsonContext` — for `AgentEntity`, `Decision`, `HistoryEntry`, `LogEntry`

These are used internally; you never need to reference them directly.

---

## 10. Skills System

### Defining Skills Programmatically

```csharp
using Squad.SDK.NET.Skills;

var skill = new SkillDefinition
{
    Id = "api-design",
    Name = "API Design",
    Triggers = ["api", "endpoint", "rest", "openapi"],
    AgentRoles = ["backend", "lead"],
    Content = "When designing APIs, follow RESTful conventions...",
    Confidence = SkillConfidence.High
};
```

### Loading Skills from SKILL.md Files

Create a `SKILL.md` file with YAML frontmatter:

```markdown
---
id: testing-skill
name: Test Generator
triggers: [test, unit, xunit, coverage]
agentRoles: [tester, backend]
confidence: high
---

When generating tests:
1. Use xUnit as the framework
2. Use FluentAssertions for assertions
3. Aim for 80%+ coverage
```

Load and register:

```csharp
// Load a single skill
SkillDefinition skill = await SkillLoader.LoadAsync("skills/testing/SKILL.md");

// Load all skills from a directory (recursively finds SKILL.md files)
IReadOnlyList<SkillDefinition> allSkills =
    await SkillLoader.LoadDirectoryAsync(".squad/skills");
```

### Skill Registry

```csharp
var registry = provider.GetRequiredService<SkillRegistry>();

// Register skills
registry.Register(skill);

// Look up a skill by ID
SkillDefinition? found = registry.Get("testing-skill");

// Get all registered skills
IReadOnlyList<SkillDefinition> all = registry.GetAll();

// Match skills to a task description (with optional role filter)
IReadOnlyList<SkillMatch> matches = registry.Match(
    task: "Write unit tests for the auth module",
    agentRole: "tester");

foreach (var match in matches)
{
    Console.WriteLine($"Skill: {match.Skill.Name}");
    Console.WriteLine($"Score: {match.Score:F2}");
    Console.WriteLine($"Reason: {match.Reason}");
}

// Load skill content by ID
string? content = registry.LoadContent("testing-skill");

// Unregister
registry.Unregister("testing-skill");
```

### Skill Security Scanner

The `SkillSecurityScanner` performs static analysis on skill markdown content to detect common security anti-patterns before skills are loaded into the registry. It ports the security review patterns from the upstream [`bradygaster/squad`](https://github.com/bradygaster/squad) project.

```csharp
using Squad.SDK.NET.Skills;

// Scan a skill file on disk
IReadOnlyList<SkillSecurityFinding> findings =
    SkillSecurityScanner.ScanContent(
        content: File.ReadAllText("skills/deploy/SKILL.md"),
        filePath: "skills/deploy/SKILL.md");

if (findings.Count == 0)
{
    Console.WriteLine("No security issues found.");
}
else
{
    foreach (var finding in findings)
    {
        Console.WriteLine($"[{finding.Severity}] {finding.Pattern} — {finding.Description}");
        Console.WriteLine($"  File: {finding.FilePath}");
    }
}
```

**Detection patterns (Phase 1):**

| Pattern | What It Flags |
|---------|---------------|
| `credentials` | Hardcoded secrets, API keys, passwords, tokens |
| `cred-file-reads` | Reading `.env`, `~/.aws/credentials`, `~/.ssh` keys, etc. |
| `download-exec` | `curl`/`wget` piped to `bash`, or fetching and executing arbitrary scripts |
| `priv-escalation` | `sudo`, `chmod 777`, `chown root`, `visudo`, etc. |

#### Integrating with Skill Loading

Scan skills as part of your loading pipeline to reject unsafe content:

```csharp
using Squad.SDK.NET.Skills;

async Task<SkillDefinition?> LoadVerifiedSkillAsync(string path)
{
    string content = await File.ReadAllTextAsync(path);

    var findings = SkillSecurityScanner.ScanContent(content, path);
    if (findings.Count > 0)
    {
        foreach (var f in findings)
            Console.Error.WriteLine($"SECURITY: [{f.Severity}] {f.Pattern}: {f.Description}");

        return null; // reject the skill
    }

    return await SkillLoader.LoadAsync(path);
}

// Use it:
SkillDefinition? skill = await LoadVerifiedSkillAsync(".squad/skills/deploy/SKILL.md");
if (skill is not null)
{
    registry.Register(skill);
}
```

#### Bulk Scanning a Skills Directory

```csharp
using Squad.SDK.NET.Skills;

var allFindings = new List<SkillSecurityFinding>();

foreach (string skillFile in Directory.EnumerateFiles(".squad/skills", "SKILL.md", SearchOption.AllDirectories))
{
    string content = await File.ReadAllTextAsync(skillFile);
    var findings = SkillSecurityScanner.ScanContent(content, skillFile);
    allFindings.AddRange(findings);
}

if (allFindings.Count == 0)
{
    Console.WriteLine("All skills passed security review.");
}
else
{
    Console.WriteLine($"{allFindings.Count} security issue(s) found:");
    foreach (var f in allFindings)
        Console.WriteLine($"  [{f.Severity}] {f.FilePath}: {f.Pattern} — {f.Description}");
}
```

---

## 11. Import/Export (Sharing)

### Exporting Squad Config to JSON

```csharp
using Squad.SDK.NET.Sharing;

var exporter = provider.GetRequiredService<SquadExporter>();

// Export to an in-memory object
ExportedSquad exported = exporter.Export(config, author: "Jeremy");

Console.WriteLine($"Name: {exported.Name}");
Console.WriteLine($"Version: {exported.Version}");
Console.WriteLine($"Author: {exported.Author}");
Console.WriteLine($"Agents: {exported.Agents.Count}");
Console.WriteLine($"Exported at: {exported.ExportedAt}");

// Export directly to a file
await exporter.ExportToFileAsync(config, "my-squad-export.json", author: "Jeremy");
```

### Importing Squad Config from JSON

```csharp
var importer = provider.GetRequiredService<SquadImporter>();

// Import from file
ImportResult result = await importer.ImportFromFileAsync("my-squad-export.json");

if (result.Success)
{
    Console.WriteLine(result.Message);
    Console.WriteLine($"Imported from: {result.ImportedPath}");
}
else
{
    Console.WriteLine($"Import failed: {result.Message}");
}
```

### Round-Trip: Export then Re-Hydrate Config

```csharp
// Export
ExportedSquad exported = exporter.Export(config);

// Deserialize back to SquadConfig
SquadConfig? reimported = importer.DeserializeConfig(exported);

if (reimported is not null)
{
    Console.WriteLine($"Re-imported team: {reimported.Team.Name}");
    Console.WriteLine($"Agents: {reimported.Agents.Count}");
}
```

---

## 12. Storage Providers

### IStorageProvider Interface

All storage providers implement the same interface:

```csharp
using Squad.SDK.NET.Storage;

IStorageProvider storage = provider.GetRequiredService<IStorageProvider>();

// Write
await storage.WriteAsync("agents/dev.json", "{\"name\":\"dev\"}");

// Read
string? data = await storage.ReadAsync("agents/dev.json");

// Check existence
bool exists = await storage.ExistsAsync("agents/dev.json");

// List keys with a prefix
IReadOnlyList<string> keys = await storage.ListAsync("agents/");

// Delete
await storage.DeleteAsync("agents/dev.json");

// Storage statistics
StorageStats stats = await storage.GetStatsAsync();
Console.WriteLine($"Items: {stats.ItemCount}");
Console.WriteLine($"Size: {stats.TotalSizeBytes} bytes");
Console.WriteLine($"Last modified: {stats.LastModified}");
```

### FileSystem Storage Provider

```csharp
using Microsoft.Extensions.Logging;

var logger = loggerFactory.CreateLogger<FileSystemStorageProvider>();
var fsStorage = new FileSystemStorageProvider("/path/to/squad-data", logger);

await fsStorage.WriteAsync("config/team.json", "{ ... }");
```

### InMemory Storage Provider

Ideal for testing and ephemeral sessions:

```csharp
var memStorage = new InMemoryStorageProvider();
await memStorage.WriteAsync("key", "value");
string? val = await memStorage.ReadAsync("key"); // "value"
```

### State Management (SquadState)

`SquadState` provides typed collections backed by any `IStorageProvider`:

```csharp
using Squad.SDK.NET.State;

var state = provider.GetRequiredService<SquadState>();

// Store an agent entity
await state.Agents.SetAsync("dev", new AgentEntity
{
    Name = "dev",
    Role = "backend",
    Status = "active",
    Model = "claude-sonnet-4.6",
    Expertise = ["C#", ".NET"]
});

// Retrieve it
AgentEntity? agent = await state.Agents.GetAsync("dev");

// List all agent keys
IReadOnlyList<string> agentKeys = await state.Agents.ListKeysAsync();

// Record a decision
await state.Decisions.SetAsync("arch-001", new Decision
{
    Id = "arch-001",
    Title = "Use Clean Architecture",
    Description = "Adopt Clean Architecture for the API layer.",
    Rationale = "Better testability and separation of concerns.",
    AgentName = "lead",
    Status = DecisionStatus.Accepted,
    Tags = ["architecture", "patterns"]
});

// Record history
await state.History.SetAsync("h-001", new HistoryEntry
{
    Id = "h-001",
    AgentName = "dev",
    Action = "created-file",
    Details = "Created src/Auth/LoginService.cs"
});

// Record logs
await state.Logs.SetAsync("log-001", new LogEntry
{
    Id = "log-001",
    Level = "Information",
    Message = "Agent spawned successfully",
    AgentName = "dev",
    SessionId = "session-123"
});
```

### Registering FileSystem Storage via DI

```csharp
services.AddSquadSdk(
    configure: builder => builder.WithTeam(t => t.Name("my-team")),
    useFileSystemStorage: true,
    storagePath: @"C:\data\squad-storage");
```

---

## 13. Platform Detection

### Detecting Platform

```csharp
using Squad.SDK.NET.Platform;

PlatformType platform = PlatformDetector.Detect();

switch (platform)
{
    case PlatformType.GitHub:
        Console.WriteLine("Running in a GitHub repository");
        break;
    case PlatformType.AzureDevOps:
        Console.WriteLine("Running in an Azure DevOps repository");
        break;
    case PlatformType.Local:
        Console.WriteLine("Running in a local git repository");
        break;
    case PlatformType.Unknown:
        Console.WriteLine("Not inside a git repository");
        break;
}
```

### Platform-Aware Behavior

```csharp
// Detect from a specific directory
PlatformType platform = PlatformDetector.Detect("/path/to/repo");

// Use in configuration
var config = SquadBuilder.Create()
    .WithTeam(team =>
    {
        team.Name("my-team");

        if (PlatformDetector.Detect() == PlatformType.GitHub)
            team.Description("GitHub-integrated squad");
    })
    .Build();
```

---

## 14. Multi-Squad Management

### Creating Personal Squads

```csharp
using Squad.SDK.NET.Resolution;

var manager = provider.GetRequiredService<MultiSquadManager>();

// Create a new personal squad
string squadPath = manager.CreateSquad("side-project");
Console.WriteLine($"Squad created at: {squadPath}");
```

### Listing Squads

```csharp
IReadOnlyList<string> squads = manager.ListSquads();

foreach (var name in squads)
{
    Console.WriteLine($"  Squad: {name}");
}
```

### Deleting Squads

```csharp
manager.DeleteSquad("side-project");
```

### Squad Resolution

```csharp
// Resolve by name
string? path = manager.ResolveSquadPath("side-project");

// Auto-resolve (finds nearest .squad/ or personal dir)
string? autoPath = manager.ResolveSquadPath();
```

### SquadResolver Static Methods

```csharp
// Find the nearest .squad/ directory by walking up the tree
ResolvedSquadPaths? resolved = SquadResolver.ResolveSquad();

if (resolved is not null)
{
    Console.WriteLine($"Mode: {resolved.Mode}");          // Project or Personal
    Console.WriteLine($"Project dir: {resolved.ProjectDir}");
    Console.WriteLine($"Personal dir: {resolved.PersonalDir}");
    Console.WriteLine($"Name: {resolved.Name}");
}

// Get the personal squad directory path
string? personalDir = SquadResolver.ResolvePersonalSquadDir();

// Ensure the personal dir exists (creates if needed)
string ensuredDir = SquadResolver.EnsurePersonalSquadDir();

// Get the global squad path
string? globalPath = SquadResolver.ResolveGlobalSquadPath();

// Check if we're inside a git worktree
bool worktree = SquadResolver.IsInsideWorktree();
```

---

## 15. Casting Engine

The casting engine assigns personas to agents from configurable "universes."

### Casting an Agent

```csharp
using Squad.SDK.NET.Casting;

var engine = provider.GetRequiredService<CastingEngine>();

CastMember member = engine.Cast(
    agentName: "backend-dev",
    roleId: "backend",
    preferredUniverse: "star-wars");

Console.WriteLine($"Persona: {member.Name}");       // "backend-dev-star-wars"
Console.WriteLine($"Universe: {member.Universe}");   // "star-wars"
Console.WriteLine($"Traits: {string.Join(", ", member.Traits)}");
```

### Managing Casts

```csharp
// Get a specific cast
CastingRecord? record = engine.GetCast("backend-dev");
if (record is not null)
{
    Console.WriteLine($"Agent: {record.AgentName}");
    Console.WriteLine($"Role: {record.RoleId}");
    Console.WriteLine($"Assigned at: {record.AssignedAt}");
}

// Get all casts
IReadOnlyList<CastingRecord> allCasts = engine.GetAllCasts();

// Remove a specific cast
engine.RemoveCast("backend-dev");

// Clear all casts
engine.ClearAll();
```

### Overflow Strategies

```csharp
// Rotate: evicts the oldest cast when capacity is reached
var rotateConfig = new CastingConfig
{
    AllowlistUniverses = ["marvel", "dc"],
    OverflowStrategy = OverflowStrategy.Rotate,
    Capacity = 3
};

// Reject: throws InvalidOperationException when capacity is reached
var rejectConfig = new CastingConfig
{
    OverflowStrategy = OverflowStrategy.Reject,
    Capacity = 5
};

// Update the engine's config at runtime
engine.UpdateConfig(rotateConfig);
```

---

## 16. Advanced Patterns

### Combining Hooks with Event Bus for Audit Logging

```csharp
var eventBus = provider.GetRequiredService<IEventBus>();
var pipeline = provider.GetRequiredService<IHookPipeline>();

// Pre-hook: log every tool invocation
pipeline.AddPreToolHook(async context =>
{
    await eventBus.EmitAsync(new SquadEvent
    {
        Type = SquadEventType.SessionToolCall,
        AgentName = context.AgentName,
        SessionId = context.SessionId,
        Payload = new ToolCallPayload
        {
            ToolName = context.ToolName,
            Arguments = context.Arguments,
            Status = ToolCallStatus.Running
        }
    });

    return PreToolUseResult.Allow();
});

// Post-hook: log completions and scrub PII
var scrubber = provider.GetRequiredService<PiiScrubberHook>();
pipeline.AddPostToolHook(scrubber.CreateHook());

pipeline.AddPostToolHook(async context =>
{
    await eventBus.EmitAsync(new SquadEvent
    {
        Type = SquadEventType.SessionToolCall,
        AgentName = context.AgentName,
        SessionId = context.SessionId,
        Payload = new ToolCallPayload
        {
            ToolName = context.ToolName,
            Status = ToolCallStatus.Completed
        }
    });

    return PostToolUseResult.Ok();
});
```

### Custom Routing with Coordinator

```csharp
var coordinator = provider.GetRequiredService<ICoordinator>();
await coordinator.InitializeAsync();

// Route different task types to different agents
string[] tasks =
[
    "Fix the null reference in UserService",            // bug-fix
    "Design the new payments architecture",             // architecture
    "Write unit tests for the auth module",             // testing
    "Update the README with new API docs"               // documentation
];

foreach (var task in tasks)
{
    var decision = await coordinator.RouteAsync(task);
    Console.WriteLine($"Task: {task[..40]}...");
    Console.WriteLine($"  → Agents: {string.Join(", ", decision.Agents)}");
    Console.WriteLine($"  → Tier: {decision.Tier}");
    Console.WriteLine($"  → Parallel: {decision.Parallel}");
    Console.WriteLine();
}
```

### Multi-Agent Fan-Out with Cost Tracking

```csharp
var agentManager = provider.GetRequiredService<IAgentSessionManager>();
var eventBus = provider.GetRequiredService<IEventBus>();
var costTracker = new CostTracker();

// Track costs from usage events
eventBus.Subscribe(SquadEventType.Usage, async evt =>
{
    if (evt.Payload is UsagePayload usage)
    {
        costTracker.RecordUsage(
            usage.Model,
            evt.SessionId ?? "unknown",
            usage.InputTokens,
            usage.OutputTokens);
    }
});

// Spawn agents for parallel investigation
var charters = new[]
{
    new AgentCharter
    {
        Name = "perf-analyst",
        Role = "backend",
        Prompt = "Analyze CPU hotspots.",
        ModelPreference = Constants.Models.ClaudeSonnet
    },
    new AgentCharter
    {
        Name = "mem-analyst",
        Role = "backend",
        Prompt = "Analyze memory allocations.",
        ModelPreference = Constants.Models.Gpt5Mini
    }
};

var events = await FanOut.SpawnParallelAsync(
    agentManager,
    charters,
    "Profile the checkout endpoint for performance issues");

// Check the cost
CostSummary summary = costTracker.GetTotalSummary();
Console.WriteLine($"Total cost for parallel analysis: ${summary.TotalEstimatedCost:F4}");
Console.WriteLine($"Models used: {string.Join(", ", summary.ByModel.Keys)}");
```

### End-to-End: DI Setup, Agent Spawn, Message, and Teardown

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Agents;
using Squad.SDK.NET.Builder;
using Squad.SDK.NET.Config;
using Squad.SDK.NET.Extensions;
using Squad.SDK.NET.Events;

// 1. Configure services
var services = new ServiceCollection();
services.AddLogging(l => l.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddSquadSdk(builder =>
{
    builder
        .WithTeam(t => t.Name("demo-squad").DefaultModel("gpt-5"))
        .WithAgent(a => a.Name("coder").Role("backend").Prompt("You write C# code."))
        .WithRouting(r =>
            r.AddRule(WorkType.FeatureDev, ["coder"], priority: 10)
             .DefaultAgent("coder")
             .Fallback(RoutingFallbackBehavior.DefaultAgent));
});

await using var provider = services.BuildServiceProvider();

// 2. Start the client
var client = provider.GetRequiredService<ISquadClient>();
await client.StartAsync();

// 3. Subscribe to events
var eventBus = provider.GetRequiredService<IEventBus>();
using var _ = eventBus.SubscribeAll(async evt =>
    Console.WriteLine($"[Event] {evt.Type} agent={evt.AgentName}"));

// 4. Spawn an agent
var agentManager = provider.GetRequiredService<IAgentSessionManager>();
var info = await agentManager.SpawnAsync(new AgentCharter
{
    Name = "coder",
    Role = "backend",
    Prompt = "You write C# code.",
    ModelPreference = "gpt-5"
});

// 5. Send a message
var agentSession = (agentManager as AgentSessionManager)?.GetSession("coder");
if (agentSession is not null)
{
    var response = await agentSession.SendAndWaitAsync(
        new SquadMessageOptions { Prompt = "Write a FizzBuzz function in C#." },
        timeout: TimeSpan.FromMinutes(2));

    Console.WriteLine($"Response:\n{response}");
}

// 6. Tear down
await agentManager.DestroyAsync("coder");
await client.StopAsync();
```

---

## Appendix: Work Type Constants

The SDK provides predefined work type constants in `Squad.SDK.NET.Config.WorkType`:

| Constant | Value |
|---|---|
| `WorkType.FeatureDev` | `"feature-dev"` |
| `WorkType.BugFix` | `"bug-fix"` |
| `WorkType.Testing` | `"testing"` |
| `WorkType.Documentation` | `"documentation"` |
| `WorkType.Refactoring` | `"refactoring"` |
| `WorkType.Architecture` | `"architecture"` |
| `WorkType.Research` | `"research"` |
| `WorkType.Triage` | `"triage"` |
| `WorkType.Meta` | `"meta"` |

## Appendix: Timeout Constants

```csharp
TimeSpan sessionCreate = Constants.Timeouts.SessionCreate;  // 30 seconds
TimeSpan messageSend   = Constants.Timeouts.MessageSend;    // 5 minutes
TimeSpan agentSpawn    = Constants.Timeouts.AgentSpawn;      // 60 seconds
TimeSpan shutdown      = Constants.Timeouts.Shutdown;        // 15 seconds
```

## Appendix: Agent Role Constants

```csharp
string lead     = Constants.AgentRoles.Lead;      // "lead"
string frontend = Constants.AgentRoles.Frontend;   // "frontend"
string backend  = Constants.AgentRoles.Backend;    // "backend"
string tester   = Constants.AgentRoles.Tester;     // "tester"
string scribe   = Constants.AgentRoles.Scribe;     // "scribe"
```
