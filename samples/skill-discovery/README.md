# skill-discovery

> **Difficulty**: Intermediate

Demonstrates loading `SKILL.md` knowledge files, registering them in a `SkillRegistry`, matching skills to tasks by trigger keywords and role affinity, discovering new patterns at runtime, and the confidence lifecycle. No `GITHUB_TOKEN` required.

## Quick start

```bash
cd samples/skill-discovery
dotnet run
```

## What you'll learn

- `SkillLoader.LoadDirectoryAsync()` — load `SKILL.md` files from nested subdirectories (each skill lives in `<skills-dir>/<skill-name>/SKILL.md`)
- `SkillRegistry.Register()` / `Match()` — register and query skills
- Runtime skill discovery — write a new `SKILL.md` and register it live
- Confidence levels: `low` → `medium` → `high`

## SKILL.md layout

Each skill lives in its own subdirectory under the shared skills folder:

```
.squad/skills/
  typescript-patterns/SKILL.md
  architecture-patterns/SKILL.md
  quality-practices/SKILL.md
```

`SkillLoader.LoadDirectoryAsync()` recursively finds every file named exactly `SKILL.md`.

## SKILL.md format

```markdown
---
id: typescript-patterns
name: TypeScript Patterns
triggers: [typescript, types, generics]
agentRoles: [developer, lead]
confidence: low
---

## TypeScript Patterns
Prefer `unknown` over `any` for type-safe narrowing.
```

## Next steps

- [hook-governance](../hook-governance/README.md) — governance and policy enforcement
- [knock-knock](../knock-knock/README.md) — live streaming agent interactions
