## Learnings

### 2026-04-04 — Skyflow Reference Removal
- Removed all "Skyflow" references from 16 files across `src/` and `.squad/`
- Source code: updated storage path (`"Squad"`), greeting/version strings in DirectResponse, and license text in README
- Team files: updated origin references in team.md, decisions.md, identity/now.md, identity/wisdom.md, and all 8 SKILL.md files
- Replacement pattern: "inherited from Skyflow project" → "established for Squad.SDK.NET"
- Build verified clean (0 errors), grep confirmed zero remaining matches
- Lesson: When extracting a project, provenance references tend to be scattered across team metadata, not just code. A systematic grep-first approach catches them all.
