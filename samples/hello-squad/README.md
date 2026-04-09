# hello-squad

> **Difficulty**: Beginner

Demonstrates how to resolve a `.squad/` directory, cast a themed team from *The Usual Suspects* universe, onboard agents with charter files, and verify deterministic casting. No `GITHUB_TOKEN` required.

## Quick start

```bash
cd samples/hello-squad
dotnet run
```

## What you'll learn

- `SquadResolver.ResolveSquad()` — find or create a `.squad/` directory
- `CastingEngine` — cast agents with a constrained universe allowlist
- Agent onboarding — write `CHARTER.md` / `HISTORY.md` per agent
- Deterministic casting — same config produces the same universe

## Expected output

```
🎬 hello-squad — Squad SDK beginner sample (.NET)

────────────────────────────────────────────────────────────
  Step 1 — Resolve .squad/ directory
────────────────────────────────────────────────────────────
  ✅ Created demo .squad/ at: /tmp/hello-squad-demo-<guid>/.squad
     ResolveSquad() → /tmp/hello-squad-demo-<guid>/.squad

  Step 2 — Cast a team from "The Usual Suspects"
  ...
  Step 5 — Casting history (persistent names)
  Casting records: 8
  Names consistent across casts: ✅ Yes
```

## Next steps

- [knock-knock](../knock-knock/README.md) — streaming multi-session patterns
- [hook-governance](../hook-governance/README.md) — governance hooks
