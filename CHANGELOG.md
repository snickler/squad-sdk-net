# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- `SkillSecurityScanner` — static analysis for skill markdown files; detects hardcoded credentials, credential file reads, download-and-execute patterns, and privilege escalation attempts (ports upstream `scripts/security-review.mjs` Phase 1 patterns)
- 70 new unit tests for `SkillSecurityScanner` (582 total)
- Compiled close-fence regex cache in `ConcurrentDictionary` to avoid recompilation on repeated calls

### Infrastructure
- Two-phase upstream sync workflow: `sync-check.yml` (Phase 1 — detect upstream `dev`/`insider` changes, open/update sync issue) and `sync-insider-cherry-pick.yml` (Phase 2 — cherry-pick insider changes after dev port)
- Squad automation workflows: `squad-triage.yml`, `squad-issue-assign.yml`, `squad-heartbeat.yml` (Ralph), `sync-squad-labels.yml`
- Sync state tracking in `.sync/upstream-state.json` with `dev.lastPortedSha` / `insider.lastPortedSha`

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
