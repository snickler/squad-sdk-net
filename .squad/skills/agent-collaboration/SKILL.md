---
name: agent-collaboration
description: Multi-agent collaboration patterns for Squad members
domain: team-collaboration
confidence: high
source: established for Squad.SDK.NET
---

## Patterns

### Worktree Awareness
- Always check `git branch --show-current` before starting work
- Never assume you're on `main` — you may be on a feature branch
- Commit to the current branch, don't create new branches unless explicitly asked

### Decision Recording
- Record all architectural decisions in `.squad/decisions.md`
- Use the format: `| Date | Decision | Context | Decided By |`
- Decisions are append-only — never edit or remove past decisions

### Cross-Agent Communication
- When your work depends on another agent, state the dependency clearly
- Don't block — if a dependency isn't ready, work on something else
- Use `mode: "background"` for long-running work so others aren't blocked

### Handoff Protocol
- When handing off work, provide: what was done, what remains, and any blockers
- Leave the codebase in a buildable state after every handoff
- Run tests before handing off

## Anti-Patterns
- ❌ Silently modifying another agent's domain without coordination
- ❌ Making decisions without recording them
- ❌ Leaving broken builds for the next agent
- ❌ Assuming context — always verify current state before acting
