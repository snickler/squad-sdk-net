<#
.SYNOPSIS
    Bumps the Squad.SDK.NET package version in the csproj and CHANGELOG.md.

.DESCRIPTION
    Updates <Version> in src/Squad.SDK.NET/Squad.SDK.NET.csproj to the
    supplied version string and prepares CHANGELOG.md for the new release.

    For stable releases (no prerelease suffix) it converts the [Unreleased]
    section into a versioned entry. For prerelease versions it inserts a new
    [Unreleased] placeholder so the next cycle has a home for notes.

.PARAMETER Version
    Target version string without a leading 'v'.  Must be valid SemVer 2.0:
      MAJOR.MINOR.PATCH  or  MAJOR.MINOR.PATCH-<prerelease>
    Examples: 0.2.0   0.2.0-preview.1   1.0.0-rc.1

.PARAMETER NoChangelog
    Skip CHANGELOG.md edits (useful when calling from other scripts that
    handle the changelog themselves).

.EXAMPLE
    # Bump to next preview on the dev branch
    ./scripts/bump-version.ps1 -Version 0.2.0-preview.1

.EXAMPLE
    # Bump to stable for a release
    ./scripts/bump-version.ps1 -Version 0.2.0

.LINK
    VERSIONING.md
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [switch]$NoChangelog
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── Helpers ────────────────────────────────────────────────────────────────

function Assert-SemVer([string]$v) {
    if ($v -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9]+(\.[a-zA-Z0-9]+)*)?$') {
        throw "Invalid SemVer: '$v'. Expected MAJOR.MINOR.PATCH[-prerelease] (e.g. 0.2.0 or 0.2.0-preview.1)."
    }
}

function Get-RepoRoot {
    $here = $PSScriptRoot
    # Walk up until we find the .git directory or the sln file
    $dir = $here
    while ($dir -and -not (Test-Path (Join-Path $dir '.git')) -and -not (Test-Path (Join-Path $dir 'Squad.SDK.NET.slnx'))) {
        $dir = Split-Path $dir -Parent
    }
    if (-not $dir) { throw "Could not locate repository root from '$here'." }
    return $dir
}

# ── Validate ────────────────────────────────────────────────────────────────

Assert-SemVer $Version

$isPreRelease = $Version -match '-'
$root = Get-RepoRoot

$csprojPath    = Join-Path $root 'src/Squad.SDK.NET/Squad.SDK.NET.csproj'
$changelogPath = Join-Path $root 'CHANGELOG.md'

if (-not (Test-Path $csprojPath)) {
    throw "csproj not found at '$csprojPath'."
}

# ── Read current version ─────────────────────────────────────────────────

$xml = [xml](Get-Content $csprojPath -Raw)
$currentVersion = $xml.Project.PropertyGroup.Version
if (-not $currentVersion) {
    throw "Could not read <Version> from '$csprojPath'."
}

Write-Host "Current version : $currentVersion"
Write-Host "New version     : $Version"

if ($currentVersion -eq $Version) {
    Write-Warning "Version is already '$Version'. Nothing to do."
    exit 0
}

# ── Update csproj ────────────────────────────────────────────────────────

if ($PSCmdlet.ShouldProcess($csprojPath, "Set <Version> to $Version")) {
    $content = Get-Content $csprojPath -Raw
    $updated = $content -replace "<Version>[^<]*</Version>", "<Version>$Version</Version>"
    if ($updated -eq $content) {
        throw "Could not find <Version> element in '$csprojPath' to replace."
    }
    Set-Content $csprojPath $updated -NoNewline
    Write-Host "✅ Updated csproj: $Version"
}

# ── Update CHANGELOG.md ─────────────────────────────────────────────────

if (-not $NoChangelog) {
    if (-not (Test-Path $changelogPath)) {
        Write-Warning "CHANGELOG.md not found at '$changelogPath' — skipping."
    } else {
        $changelog = Get-Content $changelogPath -Raw

        $today      = (Get-Date -Format 'yyyy-MM-dd')
        $UNRELEASED = '## [Unreleased]'
        $newHeader  = "## [$Version] - $today"

        if ($isPreRelease) {
            # For prerelease bumps: ensure [Unreleased] section exists at the top.
            if (-not $changelog.Contains($UNRELEASED)) {
                $firstVersionIdx = $changelog.IndexOf("`n## [")
                $placeholder = "$UNRELEASED`n`n"
                if ($firstVersionIdx -ge 0) {
                    $changelog = $changelog.Substring(0, $firstVersionIdx + 1) + $placeholder + $changelog.Substring($firstVersionIdx + 1)
                } else {
                    $changelog += "`n$placeholder"
                }
                if ($PSCmdlet.ShouldProcess($changelogPath, "Add [Unreleased] placeholder")) {
                    Set-Content $changelogPath $changelog -NoNewline
                    Write-Host "✅ Added [Unreleased] placeholder to CHANGELOG.md"
                }
            } else {
                Write-Host "ℹ️  [Unreleased] section already present — no CHANGELOG edit needed for prerelease."
            }
        } else {
            # For stable releases: convert [Unreleased] → versioned entry.
            if ($changelog.Contains($UNRELEASED)) {
                $changelog = $changelog.Replace($UNRELEASED, $newHeader)
                if ($PSCmdlet.ShouldProcess($changelogPath, "Replace [Unreleased] with [$Version] - $today")) {
                    Set-Content $changelogPath $changelog -NoNewline
                    Write-Host "✅ Promoted [Unreleased] → [$Version] in CHANGELOG.md"
                }
                # Add a fresh [Unreleased] block above the new versioned entry
                $refreshed = Get-Content $changelogPath -Raw
                $versionIdx = $refreshed.IndexOf($newHeader)
                if ($versionIdx -ge 0) {
                    $refreshed = $refreshed.Substring(0, $versionIdx) + "$UNRELEASED`n`n" + $refreshed.Substring($versionIdx)
                    if ($PSCmdlet.ShouldProcess($changelogPath, "Add fresh [Unreleased] section above [$Version]")) {
                        Set-Content $changelogPath $refreshed -NoNewline
                        Write-Host "✅ Added fresh [Unreleased] section for future work"
                    }
                }
            } else {
                Write-Warning "No [Unreleased] section found in CHANGELOG.md — inserting versioned entry at the top of the release list."
                $firstVersionIdx = $changelog.IndexOf("`n## [")
                $entry = "$UNRELEASED`n`n$newHeader`n`n### Changed`n- (add release notes here)`n`n"
                if ($firstVersionIdx -ge 0) {
                    $changelog = $changelog.Substring(0, $firstVersionIdx + 1) + $entry + $changelog.Substring($firstVersionIdx + 1)
                } else {
                    $changelog += "`n$entry"
                }
                if ($PSCmdlet.ShouldProcess($changelogPath, "Insert [$Version] entry")) {
                    Set-Content $changelogPath $changelog -NoNewline
                }
            }
        }
    }
}

# ── Summary ──────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "Version bumped to $Version. Next steps:"
if ($isPreRelease) {
    Write-Host "  1. git add -A && git commit -m 'chore: bump version to $Version'"
    Write-Host "  2. git push"
} else {
    Write-Host "  1. Review and finalize CHANGELOG.md release notes"
    Write-Host "  2. git add -A && git commit -m 'chore: bump version to $Version'"
    Write-Host "  3. Merge to main, then tag: git tag v$Version && git push origin v$Version"
    Write-Host "  4. The release workflow handles the rest."
}
