# Versioning

Squad.SDK.NET follows [Semantic Versioning 2.0](https://semver.org/spec/v2.0.0.html) with a branch model aligned to upstream:

```text
dev -> preview -> main
```

## Version Source of Truth

The canonical SDK version lives in `src/Squad.SDK.NET/Squad.SDK.NET.csproj`:

```xml
<Version>0.1.0</Version>
```

Release automation reads the effective version from MSBuild, not from a manually pushed tag.

## Branch Roles

| Branch | Role | Version state |
|--------|------|---------------|
| `dev` | Integration branch for feature work | Holds the current stable SDK version plus pending `.changeset` entries |
| `preview` | Release-candidate branch | Holds the next stable release version with changesets already applied |
| `main` | Released branch | Matches the promoted preview candidate and triggers the stable release |

## Changesets

Release-worthy SDK changes are tracked in `.changeset/*.md` files on `dev`.

Example:

```md
---
"Squad.SDK.NET": minor
---

Add preview validation for release candidates.
```

Supported bump types:

| Type | Meaning |
|------|---------|
| `patch` | Backwards-compatible fixes |
| `minor` | Backwards-compatible features |
| `major` | Breaking public API changes |

For this repo's single-package .NET layout, changesets are applied during `dev -> preview` promotion:

1. The highest pending bump is selected (`major > minor > patch`)
2. `<Version>` in the SDK csproj is bumped
3. `CHANGELOG.md` gets a new release entry
4. Applied changeset files are removed from the promoted branch

## Automated Release Flow

### 1. Work lands on `dev`

- PRs target `dev`
- Releaseable SDK changes must include a `.changeset` entry
- CI validates the SDK and the pending changeset set

### 2. Promote `dev -> preview`

Run `.github/workflows/promote.yml` with the `dev-to-preview` stage. The workflow:

1. Merges `dev` into `preview`
2. Applies pending changesets to the SDK version + changelog
3. Pushes the release candidate to `preview`

### 3. Validate `preview`

Pushing to `preview` triggers `.github/workflows/preview.yml`, which:

1. Verifies the preview version is a stable SemVer (`MAJOR.MINOR.PATCH`)
2. Verifies the changelog contains that version
3. Ensures no pending changesets remain
4. Builds, tests, and packs the SDK

### 4. Promote `preview -> main`

Run `.github/workflows/promote.yml` with the `preview-to-main` stage. The workflow:

1. Verifies `preview` is release-ready
2. Merges `preview` into `main`
3. Syncs the release commit back into `dev`

### 5. Release from `main`

Pushing to `main` triggers `.github/workflows/release.yml`, which:

1. Reads the version from MSBuild
2. Verifies the changelog contains that version
3. Ensures there are no pending changesets
4. Skips cleanly if `v<Version>` already exists
5. Builds, tests, and packs the SDK
6. Tags the commit as `v<Version>`
7. Creates the GitHub Release
8. Publishes to NuGet.org when `NUGET_PUBLISH_ENABLED == 'true'`

## Protected Release Environment

NuGet publishing uses a GitHub **Environment** named `release`. To configure:

1. Go to **Settings → Environments → New environment**
2. Name it `release`
3. (Optional) Add **required reviewers** for manual approval
4. (Optional) Restrict to the `main` branch under **Deployment branches**
5. Add the `NUGET_API_KEY` secret

## Pre-1.0 Stability

While the version is `0.x.y`, minor version bumps may include breaking changes. The public API is not yet stable. After `1.0.0`, breaking changes will only occur in major versions.
