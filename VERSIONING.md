# Versioning

Squad.SDK.NET follows [Semantic Versioning 2.0](https://semver.org/spec/v2.0.0.html).

## Version Format

```
MAJOR.MINOR.PATCH[-PRERELEASE]
```

| Component    | When to Bump                                             |
|-------------|----------------------------------------------------------|
| **MAJOR**   | Breaking changes to public API                           |
| **MINOR**   | New features, backwards-compatible                       |
| **PATCH**   | Bug fixes, backwards-compatible                          |
| **PRERELEASE** | Preview or release-candidate builds (e.g., `preview.1`, `rc.1`) |

### Examples

| Version             | Meaning                                |
|---------------------|----------------------------------------|
| `0.1.0`             | First stable preview release           |
| `0.2.0-preview.1`   | Preview of next minor release          |
| `0.2.0-rc.1`        | Release candidate                      |
| `0.2.0`             | Stable minor release                   |
| `1.0.0`             | First production-ready release         |

## Version Source of Truth

The canonical version lives in `src/Squad.SDK.NET/Squad.SDK.NET.csproj`:

```xml
<Version>0.1.0</Version>
```

All release tooling validates that the git tag matches this value.

## Release Process

### 1. Prepare the release

```bash
# Update version in csproj
# Update CHANGELOG.md with release notes
# Commit and merge to main
```

### 2. Tag and push

```bash
git tag v0.1.0
git push origin v0.1.0
```

### 3. Automated release

Pushing a `v*` tag triggers the [release workflow](.github/workflows/release.yml), which:

1. **Validates** the tag format matches SemVer
2. **Verifies** the tagged commit is on `main`
3. **Confirms** the tag version matches the csproj `<Version>`
4. **Builds and tests** the project
5. **Packs** the NuGet package (`.nupkg` + `.snupkg`)
6. **Attests** build provenance for supply-chain security
7. **Creates** a GitHub Release with auto-generated notes
8. **Publishes** to NuGet.org (requires `NUGET_API_KEY` secret in the `release` environment)

### 4. Pre-release tags

Tags containing a hyphen (e.g., `v0.2.0-preview.1`) are automatically marked as pre-release on both GitHub and NuGet.

## Protected Release Environment

NuGet publishing uses a GitHub **Environment** named `release`. To configure:

1. Go to **Settings → Environments → New environment**
2. Name it `release`
3. (Optional) Add **required reviewers** for manual approval
4. (Optional) Restrict to the `main` branch under **Deployment branches**
5. Add the `NUGET_API_KEY` secret

## Pre-1.0 Stability

While the version is `0.x.y`, minor version bumps may include breaking changes. The public API is not yet stable. After `1.0.0`, breaking changes will only occur in major versions.
