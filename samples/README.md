# samples

Learn the Squad SDK (.NET) by example. Each sample is a complete, working console application demonstrating core patterns: agent casting, session management, streaming responses, governance, and skill discovery.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **GitHub Token** (optional, for live LLM mode): `GITHUB_TOKEN=ghp_...` enables Copilot session samples

## Samples

| Sample | Difficulty | Description | Key Concepts |
|--------|-----------|-------------|--------------|
| [hello-squad](hello-squad/) | Beginner | Resolve `.squad/`, cast a team, onboard agents, print roster | `SquadResolver`, `CastingEngine` |
| [knock-knock](knock-knock/) | Beginner | Two agents trade knock-knock jokes via live Copilot sessions | `SquadClient`, `ISquadSession`, streaming events |
| [hook-governance](hook-governance/) | Intermediate | File guards, PII scrubbing, reviewer lockout, rate limiting | `HookPipeline`, `PolicyConfig`, `PiiScrubberHook` |
| [skill-discovery](skill-discovery/) | Intermediate | Load, register, and match `SKILL.md` knowledge files | `SkillLoader`, `SkillRegistry`, confidence lifecycle |

## Quick start

```bash
cd samples/hello-squad
dotnet run
```

The `knock-knock` sample requires `GITHUB_TOKEN` for live LLM mode; without it a built-in demo runs. All other samples run without external dependencies.

## Recommended learning path

1. `hello-squad` — Understand casting and agent onboarding
2. `knock-knock` — See streaming and multi-session patterns  
3. `hook-governance` — Implement security and governance patterns
4. `skill-discovery` — Explore team knowledge sharing
