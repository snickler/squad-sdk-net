param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('status', 'apply')]
    [string]$Operation,

    [string]$RepoRoot = (Get-Location).Path,

    [string]$PackageId = 'Squad.SDK.NET',

    [string]$ProjectFile = 'src/Squad.SDK.NET/Squad.SDK.NET.csproj',

    [string]$ChangelogFile = 'CHANGELOG.md',

    [string]$ReleaseDate = (Get-Date -Format 'yyyy-MM-dd'),

    [switch]$RequireNoPending
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-RepoPath {
    param([string]$RelativePath)

    return [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $RelativePath))
}

function Write-Utf8File {
    param(
        [string]$Path,
        [string]$Content
    )

    $encoding = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

function Get-ChangesetPaths {
    $changesetRoot = Resolve-RepoPath '.changeset'
    if (-not (Test-Path $changesetRoot)) {
        return @()
    }

    return @(Get-ChildItem -Path $changesetRoot -Filter '*.md' -File |
        Where-Object { $_.Name -ne 'README.md' } |
        Sort-Object Name)
}

function Get-CurrentVersion {
    $projectPath = Resolve-RepoPath $ProjectFile
    [xml]$xml = Get-Content -LiteralPath $projectPath -Raw
    $versionNode = $xml.SelectSingleNode('/Project/PropertyGroup/Version')
    if ($null -eq $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
        throw "Could not find <Version> in $ProjectFile."
    }

    return $versionNode.InnerText.Trim()
}

function Set-CurrentVersion {
    param([string]$NewVersion)

    $projectPath = Resolve-RepoPath $ProjectFile
    $document = [System.Xml.XmlDocument]::new()
    $document.PreserveWhitespace = $true
    $document.Load($projectPath)

    $versionNode = $document.SelectSingleNode('/Project/PropertyGroup/Version')
    if ($null -eq $versionNode) {
        throw "Could not find <Version> in $ProjectFile."
    }

    $versionNode.InnerText = $NewVersion

    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Indent = $true
    $settings.OmitXmlDeclaration = $true
    $settings.NewLineChars = "`n"
    $settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace
    $settings.Encoding = [System.Text.UTF8Encoding]::new($false)

    $writer = [System.Xml.XmlWriter]::Create($projectPath, $settings)
    try {
        $document.Save($writer)
    }
    finally {
        $writer.Dispose()
    }
}

function Parse-VersionComponents {
    param([string]$Version)

    $match = [regex]::Match($Version, '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-[0-9A-Za-z.-]+)?$')
    if (-not $match.Success) {
        throw "Version '$Version' is not a supported semantic version."
    }

    return [pscustomobject]@{
        Major = [int]$match.Groups['major'].Value
        Minor = [int]$match.Groups['minor'].Value
        Patch = [int]$match.Groups['patch'].Value
    }
}

function Get-NextVersion {
    param(
        [string]$CurrentVersion,
        [string]$ReleaseType
    )

    $parsed = Parse-VersionComponents $CurrentVersion
    switch ($ReleaseType) {
        'major' {
            return '{0}.{1}.{2}' -f ($parsed.Major + 1), 0, 0
        }
        'minor' {
            return '{0}.{1}.{2}' -f $parsed.Major, ($parsed.Minor + 1), 0
        }
        'patch' {
            return '{0}.{1}.{2}' -f $parsed.Major, $parsed.Minor, ($parsed.Patch + 1)
        }
        default {
            throw "Unsupported release type '$ReleaseType'."
        }
    }
}

function Get-ReleasePriority {
    param([string]$ReleaseType)

    switch ($ReleaseType) {
        'patch' { return 1 }
        'minor' { return 2 }
        'major' { return 3 }
        default { throw "Unsupported release type '$ReleaseType'." }
    }
}

function Normalize-Summary {
    param([string]$Body)

    $normalized = ($Body -replace '\r?\n+', ' ').Trim()
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        throw 'Changeset body cannot be empty.'
    }

    return $normalized
}

function Get-ChangesetRecords {
    $records = [System.Collections.Generic.List[object]]::new()

    foreach ($file in Get-ChangesetPaths) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        $match = [regex]::Match($content, '^(?s)---\r?\n(?<frontmatter>.*?)\r?\n---\r?\n(?<body>.*)$')
        if (-not $match.Success) {
            throw "Changeset '$($file.Name)' must contain markdown frontmatter."
        }

        $frontmatter = $match.Groups['frontmatter'].Value -split '\r?\n'
        $releaseType = $null
        foreach ($line in $frontmatter) {
            $trimmed = $line.Trim()
            if ([string]::IsNullOrWhiteSpace($trimmed)) {
                continue
            }

            $entry = [regex]::Match($trimmed, '^(?:"(?<double>[^"]+)"|''(?<single>[^'']+)''|(?<bare>[^:]+))\s*:\s*(?<type>major|minor|patch)\s*$')
            if (-not $entry.Success) {
                throw "Changeset '$($file.Name)' has an invalid frontmatter line: $trimmed"
            }

            $name = $entry.Groups['double'].Value
            if ([string]::IsNullOrWhiteSpace($name)) {
                $name = $entry.Groups['single'].Value
            }
            if ([string]::IsNullOrWhiteSpace($name)) {
                $name = $entry.Groups['bare'].Value.Trim()
            }

            if ($name -eq $PackageId) {
                if ($null -ne $releaseType) {
                    throw "Changeset '$($file.Name)' declares '$PackageId' more than once."
                }

                $releaseType = $entry.Groups['type'].Value.ToLowerInvariant()
            }
        }

        if ($null -eq $releaseType) {
            throw "Changeset '$($file.Name)' must declare '$PackageId' in the frontmatter."
        }

        $records.Add([pscustomobject]@{
                FileName = $file.Name
                FullPath = $file.FullName
                ReleaseType = $releaseType
                Summary = Normalize-Summary $match.Groups['body'].Value
            })
    }

    return @($records.ToArray())
}

function Get-HighestReleaseType {
    param([object[]]$Records)

    if ($Records.Count -eq 0) {
        return $null
    }

    return ($Records |
        Sort-Object @{ Expression = { Get-ReleasePriority $_.ReleaseType }; Descending = $true } |
        Select-Object -First 1).ReleaseType
}

function Update-Changelog {
    param(
        [string]$Version,
        [object[]]$Records
    )

    $changelogPath = Resolve-RepoPath $ChangelogFile
    $existing = Get-Content -LiteralPath $changelogPath -Raw

    $escapedVersion = [regex]::Escape($Version)
    if ([regex]::IsMatch($existing, "(?m)^## \[$escapedVersion\]")) {
        Write-Host "CHANGELOG.md already contains [$Version]."
        return
    }

    $entryLines = [System.Collections.Generic.List[string]]::new()
    $entryLines.Add("## [$Version] - $ReleaseDate")
    $entryLines.Add('')
    $entryLines.Add('### Changed')
    foreach ($record in $Records) {
        $entryLines.Add("- $($record.Summary)")
    }
    $entryLines.Add('')
    $entryLines.Add('')
    $entryText = ($entryLines -join "`n")

    # Find the Unreleased section to insert after it (Keep-a-Changelog standard)
    $unreleasedMatch = [regex]::Match($existing, '(?m)^## \[Unreleased\]')
    if ($unreleasedMatch.Success) {
        # Find the next ## [ heading after Unreleased (the previous release)
        $nextReleaseMatch = [regex]::Match($existing, '(?m)^## \[', $unreleasedMatch.Index + $unreleasedMatch.Length)
        if ($nextReleaseMatch.Success) {
            # Insert between Unreleased and the next release
            $updated = $existing.Insert($nextReleaseMatch.Index, $entryText)
        }
        else {
            # No existing releases, append after Unreleased section
            $updated = $existing.TrimEnd() + "`n`n" + $entryText
        }
    }
    else {
        # No Unreleased section, fall back to inserting before first ## [ (backward compatibility)
        $match = [regex]::Match($existing, '(?m)^## \[')
        if ($match.Success) {
            $updated = $existing.Insert($match.Index, $entryText)
        }
        else {
            $updated = $existing.TrimEnd() + "`n`n" + $entryText
        }
    }

    Write-Utf8File -Path $changelogPath -Content $updated
}

$records = @(Get-ChangesetRecords)
$currentVersion = Get-CurrentVersion
$highestReleaseType = Get-HighestReleaseType $records

if ($RequireNoPending -and $records.Count -gt 0) {
    throw "Pending changesets are still present: $($records.FileName -join ', ')"
}

switch ($Operation) {
    'status' {
        Write-Host "PendingChangesets: $($records.Count)"
        Write-Host "CurrentVersion: $currentVersion"

        if ($null -eq $highestReleaseType) {
            Write-Host 'ReleaseType: none'
            Write-Host 'NextVersion: none'
            return
        }

        $nextVersion = Get-NextVersion -CurrentVersion $currentVersion -ReleaseType $highestReleaseType
        Write-Host "ReleaseType: $highestReleaseType"
        Write-Host "NextVersion: $nextVersion"
        foreach ($record in $records) {
            Write-Host "- $($record.FileName): $($record.ReleaseType) - $($record.Summary)"
        }
    }
    'apply' {
        if ($null -eq $highestReleaseType) {
            Write-Host 'No pending changesets found.'
            return
        }

        $nextVersion = Get-NextVersion -CurrentVersion $currentVersion -ReleaseType $highestReleaseType
        Set-CurrentVersion -NewVersion $nextVersion
        Update-Changelog -Version $nextVersion -Records $records

        foreach ($record in $records) {
            Remove-Item -LiteralPath $record.FullPath -Force
        }

        Write-Host "Applied $($records.Count) changeset(s)."
        Write-Host "ReleaseType: $highestReleaseType"
        Write-Host "PreviousVersion: $currentVersion"
        Write-Host "NextVersion: $nextVersion"
    }
}
