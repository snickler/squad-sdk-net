# Decision: Two-Phase Dev→Insider Sync Pipeline

**Date:** 2026-04-07  
**Decided By:** Jeremy Sinclair  
**Implemented By:** Ashford (Platform & DevOps)

## Context

Previously, the upstream sync system created a single combined issue covering both `dev` and `insider` changes. Both branches were ported independently, which created challenges:

1. **Coordination complexity** — No enforced ordering between dev and insider ports
2. **Code duplication risk** — Same upstream work might be implemented differently on each branch
3. **Insider boundary ambiguity** — Hard to determine which upstream work belongs in insider vs. dev-only

## Decision

Redesign the sync workflow into a **two-phase pipeline**:

### Phase 1: Dev Sync (`sync-check.yml`)
- Detects upstream changes and creates **dev-only** sync issues
- Labels: `sync`, `sync:dev`, `squad:copilot`
- Skips if only insider changed (phase 2 handles that scenario)
- Issue includes machine-readable markers:
  - `<!-- dev-target-sha: {upstream dev HEAD} -->`
  - `<!-- insider-target-sha: {upstream insider HEAD} -->`
  - `<!-- our-dev-head: {our origin/dev HEAD at issue creation} -->`
- Shows upstream dev↔insider delta for context (helps agent understand boundary)
- Completion: port dev changes, update `dev.lastPortedSha`

### Phase 2: Insider Cherry-Pick (`sync-insider-cherry-pick.yml`)
- Triggers on `issues: closed` event for issues with `sync:dev` label
- Reads closed issue body to extract metadata (our-dev-head, insider-target-sha)
- Computes commit range on our dev made during the port
- Lists upstream insider commits and dev-only commits (exclusion list)
- Creates insider cherry-pick issue with full context
- Copilot agent determines which dev commits to cherry-pick based on upstream boundary

## Design Principles

1. **Dev is always ported first** — Insider NEVER gets independent ports
2. **Code flows dev→insider** — Insider changes come via cherry-picks from dev
3. **Agent-driven boundary determination** — Workflow provides context, agent uses judgment
4. **Machine-readable markers** — Enable workflow chaining via issue metadata
5. **Traceability** — Encourage upstream SHA references in commit messages

## Benefits

- **Enforced ordering** — Dev must complete before insider work starts
- **Code reuse** — Insider gets exact same commits from dev (no reimplementation)
- **Clear boundary** — Agent sees upstream delta and makes informed cherry-pick decisions
- **Automation** — Phase 2 triggers automatically when phase 1 completes
- **Visibility** — Full commit history and upstream context in every issue

## Implementation

- Modified: `.github/workflows/sync-check.yml` — dev-only issue creation
- Created: `.github/workflows/sync-insider-cherry-pick.yml` — follow-up automation
- Both workflows deployed to `insider` and `dev` branches
- Tracking file: `.sync/upstream-state.json` maintains per-branch state

## Edge Cases Handled

- If upstream/insider hasn't changed, skip insider issue creation
- If closed dev issue missing markers, log warning and skip
- If no commits made on dev during port, skip insider follow-up
- If upstream force-pushed (tracked SHA not found), handle gracefully with re-baseline instructions
