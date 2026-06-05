---
name: "upstream-dev-sync"
description: "Analyze and port latest bradygaster/squad dev changes into squad-sdk-net using the sync issue format"
domain: "development"
confidence: "high"
source: "manual"
---

## Context

Use this skill when a sync issue reports new commits on `bradygaster/squad` `dev` and those changes must be ported into this repository's `dev` branch.

This is an operational CI/CD skill for this repo only.

## When To Use

- A sync issue exists with title like: `Sync: upstream dev changes — YYYY-MM-DD`
- The issue body contains:
  - `<!-- upstream-sha: ... -->`
  - `### Commits`
  - `### File Diff`
  - completion checklist for porting
- You need a repeatable process for analyzing upstream changes and aligning .NET behavior

## Hard Scope Rules

1. **Dev-only sync path.** Port from upstream `dev` to local `dev`.
2. **Do not use preview/insider sync paths.** Those branches are retired for this workflow.
3. **Reference upstream SHAs in commit messages** when practical:
   - `Port feature X (upstream/dev@a1b2c3d)`

## Inputs

- Sync issue number
- Upstream repository: `bradygaster/squad`
- Upstream branch: `dev`

## Workflow

### 1. Read the sync issue and capture target SHAs

Extract:

- target upstream SHA from `<!-- upstream-sha: ... -->`
- commit list from the `### Commits` section
- changed-file summary from the `### File Diff` section

```powershell
# Example: fetch issue body
gh issue view <ISSUE_NUMBER> --repo snickler/squad-sdk-net --json body --jq .body
```

### 2. Build a porting map before editing code

For each upstream commit:

1. identify intent (feature/fix/refactor/docs/ci)
2. find .NET analog surface(s)
3. mark action:
   - `PORT` (direct/near-direct)
   - `ADAPT` (same behavior, .NET-specific implementation)
   - `SKIP` (not relevant to .NET repo; include reason)

Use this quick mapping heuristic:

| Upstream area | Common .NET target |
|---|---|
| `.github/workflows/*` | `.github/workflows/*` |
| docs/skills/process files | `.copilot/skills/*`, `CONTRIBUTING.md`, `.squad/*` (only if explicitly required) |
| runtime/sdk behavior | `src/Squad.SDK.NET/**` |
| tests | `tests/Squad.SDK.NET.Tests/**` |

### 3. Port in small, traceable commits

- Keep commits focused by concern (workflow/docs/runtime/tests).
- Include upstream SHA references in commit messages where practical.
- Preserve behavior intent, not language-specific implementation details.

Commit message examples:

- `Port sync issue format alignment (upstream/dev@76d646b)`
- `Port preview sync disablement (upstream/dev@1a870aa)`

### 4. Validate parity and repository health

- Confirm issue-requested behavior is present in the .NET repo.
- Ensure local changes are consistent with existing repo conventions.
- Run the repo validation commands expected for the scope of your edits.

### 5. Update tracking state and issue checklist

Follow the sync issue instructions exactly:

1. mark completed checklist items
2. update `.sync/upstream-state.json` with latest `dev.lastPortedSha` **if the file exists in this repo**
3. if `.sync/upstream-state.json` does not exist, note that explicitly in the issue/PR so state handling remains transparent
4. close the issue when the port is complete

## Done Criteria

- All relevant upstream `dev` commits in the issue are ported, adapted, or explicitly skipped with rationale
- Local commits reference upstream SHAs where practical
- No preview/insider sync behavior is introduced
- Sync issue checklist is fully resolved and issue is closed
