---
name: git-workflow
description: Git branching and workflow conventions for Squad.SDK.NET
domain: version-control
confidence: high
source: adapted from Skyflow project
---

## Branch Model

### Main Branches
- `main` — Stable, release-ready code. Protected branch.
- `dev/*` — Development branches for features and fixes

### Branch Naming
```
dev/{author}/{feature-name}    — Feature branches
fix/{author}/{bug-description} — Bug fix branches
release/v{X.Y.Z}              — Release preparation branches
```

### Workflow
1. Create branch from `main`
2. Develop and test locally
3. Push and create PR to `main`
4. Reviewer approves (see `reviewer-protocol`)
5. Squash merge to `main`
6. Tag release if applicable

## Commit Conventions

### Format
```
type: short description

Longer description if needed.

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

### Types
- `feat:` — New feature or capability
- `fix:` — Bug fix
- `test:` — Test additions or updates
- `docs:` — Documentation changes
- `ci:` — CI/CD pipeline changes
- `refactor:` — Code restructuring without behavior change
- `chore:` — Maintenance tasks

## Rules
- Never force-push to `main`
- Always rebase or merge `main` into your branch before PR
- Delete branches after merge
- One logical change per PR — don't bundle unrelated changes

## Worktree Patterns
When using git worktrees:
- Check `git branch --show-current` before any work
- Commit to the current branch — don't create new branches
- The worktree path tells you which branch you're on
