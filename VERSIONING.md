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

## Version Bump Script

Use `scripts/bump-version.ps1` to update the version in one step. It updates `<Version>` in `Squad.SDK.NET.csproj` and prepares `CHANGELOG.md` automatically.

```powershell
# Bump to a prerelease (e.g., on the dev branch after a stable release)
./scripts/bump-version.ps1 -Version 0.2.0-preview.1

# Bump to stable for an upcoming release
./scripts/bump-version.ps1 -Version 0.2.0
```

**Stable release behavior:** converts the `[Unreleased]` section in `CHANGELOG.md` into a versioned entry and inserts a fresh `[Unreleased]` placeholder above it.

**Prerelease bump behavior:** ensures an `[Unreleased]` placeholder exists at the top of `CHANGELOG.md` and updates the csproj version.

## Release Process

### 1. Prepare the release

```powershell
# From the repo root (Windows, macOS, or Linux with pwsh installed)
./scripts/bump-version.ps1 -Version 0.2.0

# Review CHANGELOG.md — fill in any missing release notes
# Then commit
git add -A
git commit -m "chore: bump version to 0.2.0"
```

### 2. Merge and tag

```bash
# Merge to main (via pull request), then:
git tag v0.2.0
git push origin v0.2.0
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
9. **Publishes** to GitHub Packages (using `GITHUB_TOKEN`, no extra configuration needed)

### 4. Post-release dev sync (automated)

After a successful stable release, the [post-release sync workflow](.github/workflows/post-release-dev-sync.yml) automatically opens a pull request against `dev` that bumps the version to the next minor preview (e.g., `0.3.0-preview.1` after releasing `0.2.0`). Review and merge that PR to keep `dev` on a prerelease version.

### 5. Pre-release tags

Tags containing a hyphen (e.g., `v0.2.0-preview.1`) are automatically marked as pre-release on both GitHub and NuGet.

## Pre-release Packages on GitHub Packages

Three pre-release package channels are published automatically to [GitHub Packages](https://github.com/snickler/squad-sdk-net/packages):

| Channel | Trigger | Version format | Example |
|---------|---------|----------------|---------|
| **CI** | Every PR build | `{version}-ci.{run_number}` | `0.1.0-ci.42` |
| **Dev** | Push / merge to `dev` | `{version}-dev` | `0.1.0-dev` |
| **Release** | Push a `v*` tag | `{version}` | `0.1.0` |

To consume any of these packages, add the GitHub Packages NuGet source to your project:

```bash
dotnet nuget add source \
  "https://nuget.pkg.github.com/snickler/index.json" \
  --name "GitHub snickler" \
  --username <your-github-username> \
  --password <your-github-pat>
```

Then install the latest pre-release build:

```bash
dotnet add package Squad.SDK.NET --prerelease
```

## Protected Release Environment

NuGet publishing uses a GitHub **Environment** named `release`. To configure:

1. Go to **Settings → Environments → New environment**
2. Name it `release`
3. (Optional) Add **required reviewers** for manual approval
4. (Optional) Restrict to the `main` branch under **Deployment branches**
5. Add the `NUGET_API_KEY` secret

## Pre-1.0 Stability

While the version is `0.x.y`, minor version bumps may include breaking changes. The public API is not yet stable. After `1.0.0`, breaking changes will only occur in major versions.
