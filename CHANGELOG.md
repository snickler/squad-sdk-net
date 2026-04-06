# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

#### Scratch directory utilities (`SquadResolver`)
- `ScratchDir(squadRoot, create)` — resolves `{squadRoot}/.scratch/`, creating it on demand
- `ScratchFile(squadRoot, prefix, ext, content)` — generates a unique temp file path (with optional immediate write) inside the scratch directory; sanitizes prefix against path traversal

#### Path validation helpers (`SquadResolver`)
- `EnsureSquadPath(filePath, squadRoot)` — validates a path is within the squad root or system temp directory
- `EnsureSquadPathDual(filePath, projectDir, teamDir)` — validates against dual project+team roots + temp
- `EnsureSquadPathTriple(filePath, projectDir, teamDir, personalDir)` — validates against three roots + temp
- `EnsureSquadPathResolved(filePath, paths)` — convenience wrapper using `ResolvedSquadPaths`

#### External state storage (`SquadResolver`)
- `DeriveProjectKey(projectDir)` — derives a stable, sanitized project key from a directory path basename
- `ResolveExternalStateDir(projectKey, create)` — resolves `{personalDir}/projects/{sanitizedKey}/` for storing squad state outside the working tree; validates against path traversal

#### `SquadDirConfig` properties
- `StateLocation` — where state is stored (`"external"` means outside the working tree; `null` = local default)
- `StateBackend` — storage backend identifier (`"worktree"`, `"external"`, `"git-notes"`, `"orphan"`)

#### `PlatformType` enum
- `Planner` — Microsoft Planner task board platform

#### Communication adapter types (`Squad.SDK.NET.Platform`)
- `ICommunicationAdapter` — pluggable agent-human communication interface (`PostUpdateAsync`, `PollForRepliesAsync`, `GetNotificationUrl`)
- `CommunicationChannel` enum — `GitHubDiscussions`, `AdoWorkItems`, `TeamsGraph`, `FileLog`
- `CommunicationConfig` record — channel selection + per-session behavior flags
- `CommunicationReply` record — normalized reply from a human on any channel
- `PostUpdateOptions` / `PostUpdateResult` / `PollForRepliesOptions` — strongly-typed adapter options

#### Microsoft Teams communication adapter (`TeamsCommunicationAdapter`)
- Bidirectional Teams chat via Microsoft Graph API
- Auth flow: cached token → refresh → device code fallback (no browser required in headless environments)
- Tokens persisted in `{personalSquadDir}/teams-tokens.json` with owner-only file permissions
- Supports 1:1 chat (by recipient UPN), team channel messages, or explicit chat ID
- Retry with back-off on Graph API throttling (HTTP 429 / 503 / 504)
- `TeamsCommsConfig` record for per-adapter configuration

## [0.1.0] - 2026-04-04

### Added
- Initial release of Squad.SDK.NET
- Fluent builder API for squad configuration with immutable built configs
- Coordinator with intelligent routing and fan-out
- Event bus (pub/sub) via System.Threading.Channels
- Pre/post tool-use hook pipeline for governance
- Agent session management with pooling
- Cost tracking and usage aggregation
- Charter compiler (markdown + YAML frontmatter)
- Skill registry and loader
- Platform detection (OS, terminal, IDE)
- Import/export for portable squad configurations
- RemoteBridge with IAsyncDisposable for proper resource cleanup
- Full AOT / Native AOT compatibility
- Source-generated JSON serialization (4 contexts)
- Microsoft.Extensions.DependencyInjection integration
- SourceLink and symbol packages for source-level debugging
- Deterministic builds with TreatWarningsAsErrors
- Comprehensive XML documentation on all public APIs
- 474+ unit tests across 20+ test classes

### Infrastructure
- Multi-OS CI matrix (Ubuntu + Windows) with code coverage
- Tag-driven release workflow with SemVer validation
- Build provenance attestation for supply-chain security
- CodeQL security scanning (weekly + on PR)
- Dependency review on pull requests
- CODEOWNERS for required review enforcement
- NuGet package metadata (ready for publishing)
- Community files (CONTRIBUTING, SECURITY, issue/PR templates)
