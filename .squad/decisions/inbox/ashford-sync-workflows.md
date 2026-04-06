# Decision: Upstream Sync Workflow Architecture

**Date:** 2025-07-23
**Decided By:** Ashford (Platform & DevOps)
**Requested By:** Jeremy Sinclair

## Decision

Use a reusable `workflow_call` workflow (`sync-check.yml`) with thin caller workflows (`sync-check-dev.yml`, `sync-check-insiders.yml`) to monitor `bradygaster/squad` upstream branches for changes. Issues are created automatically in this repo when new upstream commits are detected.

## Key Design Choices

| Choice | Decision | Rationale |
|--------|----------|-----------|
| State tracking | HTML comment in issue body (`<!-- upstream-sha: ... -->`) | Inspectable, no external storage, survives cache eviction |
| Duplicate prevention | Open issue check + concurrency groups | Two independent guards against race conditions |
| Input handling | `env:` vars, not string interpolation | Prevents script injection via workflow inputs |
| Schedule | Weekdays 08:00 UTC | Matches team working hours, avoids weekend noise |
| Labels | `sync`, `sync:dev`, `sync:insiders` | Auto-created in workflow + managed centrally in `sync-squad-labels.yml` |

## Files

- `.github/workflows/sync-check.yml` — reusable core workflow
- `.github/workflows/sync-check-dev.yml` — dev branch caller
- `.github/workflows/sync-check-insiders.yml` — insiders branch caller
- `.github/workflows/sync-squad-labels.yml` — updated with sync label definitions
