---
name: "release-process"
description: "Squad.SDK.NET release runbook: changesets on dev, preview validation, main-triggered release"
domain: "release-management"
confidence: "high"
source: "team-decision"
---

## Context

Stable releases are created automatically from `main`.

No one should manually push a release tag. The release path is:

```text
dev -> preview -> main
```

## Rules

1. **Releaseable SDK changes on `dev` require a `.changeset` file.**
2. **`preview` and `main` must have no pending changesets.**
3. **`preview` and `main` must carry a stable SemVer version (`MAJOR.MINOR.PATCH`).**
4. **The source of truth is `src/Squad.SDK.NET/Squad.SDK.NET.csproj`.**
5. **`main` creates the `v<Version>` tag automatically.**

## Normal Release Flow

### 1. Merge work into `dev`

- PRs target `dev`
- SDK changes add `.changeset/*.md`
- CI validates the SDK and pending changesets

### 2. Promote `dev -> preview`

Run `promote.yml` with stage `dev-to-preview`.

That workflow:

1. merges `dev` into `preview`
2. applies pending changesets
3. bumps the SDK version in the csproj
4. writes the new changelog entry
5. removes the applied changeset files from the promoted branch

### 3. Validate `preview`

`preview.yml` must pass before the release continues.

It checks:

- stable SemVer on `preview`
- changelog entry for that version
- no pending changesets
- successful build, test, and pack

### 4. Promote `preview -> main`

Run `promote.yml` with stage `preview-to-main`.

That workflow:

1. merges `preview` into `main`
2. pushes `main`, which triggers the release workflow
3. syncs the release commit back into `dev`

### 5. Let `main` release itself

`release.yml`:

1. reads the version from MSBuild
2. verifies changelog + no pending changesets
3. skips cleanly if the tag already exists
4. builds, tests, and packs the SDK
5. creates `v<Version>`
6. creates the GitHub Release
7. publishes to NuGet.org when enabled

## Useful Commands

Check pending changesets:

```powershell
pwsh -NoProfile -File .\scripts\Changesets.ps1 -Operation status
```

Fail if a branch still has pending changesets:

```powershell
pwsh -NoProfile -File .\scripts\Changesets.ps1 -Operation status -RequireNoPending
```

## Anti-Patterns

- ❌ Manually tagging a release commit
- ❌ Editing the SDK version directly on `dev` instead of using changesets
- ❌ Promoting `preview` while `.changeset/*.md` files are still present
- ❌ Releasing from a prerelease version on `main`
