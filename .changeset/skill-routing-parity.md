---
"squad.sdk.net": patch
---

Port coordinator skill-aware routing parity from upstream dev (fe1e7e8, 8a62093)

- Coordinator agent configurations now scan 5 skill search paths in precedence order:
  1. `.squad/skills/` (team-earned, highest priority)
  2. `.copilot/skills/` (CLI-resident)
  3. `.github/skills/` (repository-standard)
  4. `.claude/skills/` (Claude-app-resident)
  5. `.agents/skills/` (legacy fallback, lowest priority)

- Traversal safety rules:
  - Single-level scan only (no recursive descent)
  - Reject symlinks and Windows reparse points
  - Ensure filesystem-neutral validation

- Skill name deduplication rules:
  - Case-insensitive comparison
  - NFC Unicode normalization
  - Trim zero-width characters
  - Reject null bytes, control characters, path separators
  - Reject Windows reserved names (CON, PRN, AUX, NUL, COM1-9, LPT1-9)

- Applied uniformly across all three coordinator template surfaces:
  - `.github/agents/squad.agent.md` (repository coordinator)
  - `.squad/templates/squad.agent.md` (team template)
  - `src/Squad.SDK.NET/Templates/squad.agent.md.template` (SDK-shipped template)
