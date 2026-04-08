# knock-knock

> **Difficulty**: Beginner

Two Copilot sessions trade knock-knock jokes, streaming token-by-token. Requires `GITHUB_TOKEN`; falls back to built-in demo mode without it.

## Prerequisites

```bash
export GITHUB_TOKEN=ghp_...   # macOS/Linux
$env:GITHUB_TOKEN = "ghp_..."  # PowerShell
```

## Quick start

```bash
cd samples/knock-knock
dotnet run
```

## What you'll learn

- `SquadClient` — connect to GitHub Copilot
- `CreateSessionAsync()` with system-prompt personalities
- `ISquadSession.On()` — subscribe to `MessageDelta` streaming events
- Multi-turn conversation state across multiple agents

## Next steps

- [hook-governance](../hook-governance/README.md) — governance and security hooks
- [skill-discovery](../skill-discovery/README.md) — team knowledge sharing
