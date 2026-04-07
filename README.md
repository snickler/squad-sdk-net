# Squad SDK .NET

A .NET SDK for multi-agent orchestration using GitHub Copilot — .NET port of [@bradygaster/squad-sdk](https://github.com/bradygaster/squad-sdk).

[![CI](https://github.com/snickler/squad-sdk-net/actions/workflows/ci.yml/badge.svg)](https://github.com/snickler/squad-sdk-net/actions/workflows/ci.yml)

## Quick Start

```bash
# Clone
git clone https://github.com/snickler/squad-sdk-net.git
cd squad-sdk-net

# Build
dotnet build Squad.SDK.NET.slnx

# Test
dotnet test Squad.SDK.NET.slnx
```

## Project Structure

```
squad-sdk-net/
├── src/
│   └── Squad.SDK.NET/          # Core SDK library (v0.1.0)
│       ├── Abstractions/       # Interfaces and contracts
│       ├── Agents/             # Agent session management
│       ├── Builder/            # Fluent configuration builders
│       ├── Casting/            # Agent casting engine
│       ├── Config/             # Configuration types
│       ├── Coordinator/        # Request routing & fan-out
│       ├── Events/             # Event bus system
│       ├── Extensions/         # DI extensions
│       ├── Hooks/              # Pre/post tool-use hooks
│       ├── Marketplace/        # Skill marketplace
│       ├── Platform/           # Platform detection
│       ├── Remote/             # Remote bridge protocol
│       ├── Resolution/         # Squad resolver & multi-squad
│       ├── Roles/              # Role catalog
│       ├── Runtime/            # Cost tracking, session pool
│       ├── Sharing/            # Import/export
│       ├── Skills/             # Skill loader & registry
│       ├── State/              # State management
│       ├── Storage/            # Storage providers
│       ├── Tools/              # Built-in tool definitions
│       └── Utils/              # String utilities
├── tests/
│   └── Squad.SDK.NET.Tests/    # Unit tests (582+ tests)
├── docs/
│   └── examples.md             # Comprehensive usage examples
├── .editorconfig               # Code style rules
├── .github/
│   ├── ISSUE_TEMPLATE/         # Bug report & feature request templates
│   ├── PULL_REQUEST_TEMPLATE.md
│   ├── workflows/
│   │   ├── ci.yml                       # CI (multi-OS matrix, coverage, pack)
│   │   ├── codeql.yml                   # CodeQL security scanning
│   │   ├── dependency-review.yml        # Dependency vulnerability review
│   │   ├── release.yml                  # Tag-driven release + NuGet publish
│   │   ├── sync-check.yml               # Phase 1: detect upstream dev changes
│   │   ├── sync-insider-cherry-pick.yml # Phase 2: cherry-pick to insider branch
│   │   ├── squad-heartbeat.yml          # Ralph: periodic triage & @copilot assign
│   │   ├── squad-triage.yml             # Auto-triage issues via squad label
│   │   ├── squad-issue-assign.yml       # Route squad:{member} labels to assignees
│   │   └── sync-squad-labels.yml        # Sync GitHub labels from team roster
├── CHANGELOG.md                # Release notes
├── CODEOWNERS                  # Required reviewers
├── CONTRIBUTING.md             # Contribution guide
├── SECURITY.md                 # Vulnerability reporting policy
├── VERSIONING.md               # Versioning scheme and release process
├── Squad.SDK.NET.slnx          # Solution file
├── Directory.Build.props       # Shared build properties (SourceLink, warnings-as-errors)
├── Directory.Packages.props    # Central package management
└── global.json                 # SDK version pinning
```

## Documentation

- **[Usage Examples](docs/examples.md)** — 16-section cookbook with copy-paste-ready C# examples covering the Builder API, routing, events, hooks, cost tracking, sessions, skills, skill security scanning, casting, import/export, and more.
- **API Documentation** — All public types, methods, and properties include comprehensive XML doc comments for IntelliSense and generated API reference.
- **[Package README](src/Squad.SDK.NET/README.md)** — Detailed feature overview, architecture diagram, and quick start guide.
- **[Changelog](CHANGELOG.md)** — Release notes following [Keep a Changelog](https://keepachangelog.com/) format.

## GitHub Automation Workflows

### CI & Release

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `ci.yml` | Push/PR to `main` | Build, test (Ubuntu + Windows), coverage, NuGet pack |
| `release.yml` | Push `v*` tag | Validate SemVer, build, attest provenance, GitHub Release, NuGet publish |
| `codeql.yml` | PR, push, weekly | CodeQL C# security scanning |
| `dependency-review.yml` | PR | Flag high-severity CVEs and restrictive licenses |

### Upstream Sync (2-Phase)

The repo tracks upstream [`bradygaster/squad`](https://github.com/bradygaster/squad) with a two-phase automated sync:

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `sync-check.yml` | Weekdays 08:00 UTC / manual | Phase 1 — detect new commits on upstream `dev` and `insider`; open/update a `sync` issue |
| `sync-insider-cherry-pick.yml` | On close of `sync:dev` issue | Phase 2 — create an `insider` cherry-pick tracking issue from the completed dev sync |

Sync state is tracked in `.sync/upstream-state.json` (`dev.lastPortedSha` / `insider.lastPortedSha`). Issues are labeled `sync:dev` or `sync:insider` and carry a `squad:copilot` label for autonomous assignment.

### Squad Automation

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `squad-triage.yml` | Issue labeled `squad` | Lead (Holden) triages and routes to a `squad:{member}` label |
| `squad-issue-assign.yml` | Issue labeled `squad:{member}` | Posts assignment acknowledgment; invokes coding agent if `squad:copilot` |
| `squad-heartbeat.yml` | Every 30 min / issue/PR events | Ralph: smart triage + @copilot auto-assign for unassigned `squad:copilot` issues |
| `sync-squad-labels.yml` | Push to `team.md` / manual | Sync GitHub labels from the `.squad/team.md` roster |

## Build & Packaging

- **SourceLink** enabled — step through NuGet package source in your debugger
- **Deterministic builds** with `ContinuousIntegrationBuild` in CI
- **Symbol packages** (`.snupkg`) for source-level debugging
- **TreatWarningsAsErrors** — zero tolerance for compiler warnings
- **CI matrix** — builds and tests on both Ubuntu and Windows
- **Tag-driven releases** — push a `v*` tag to create a GitHub Release with NuGet packages
- **Build provenance** — attestation via `actions/attest-build-provenance` for supply-chain security

See [VERSIONING.md](VERSIONING.md) for the versioning scheme and release process.

## Security

- **CodeQL** scans on every PR and weekly for C# vulnerabilities
- **Dependency review** flags high-severity CVEs and restrictive licenses on PRs
- **`SkillSecurityScanner`** — built-in static analysis for skill markdown files; detects hardcoded credentials, credential file reads, download-and-execute patterns, and privilege escalation attempts
- **CODEOWNERS** requires review from [@snickler](https://github.com/snickler)

Report vulnerabilities privately — see [SECURITY.md](SECURITY.md).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup, coding guidelines, and the pull request process.

For security vulnerabilities, see [SECURITY.md](SECURITY.md).

## Requirements

- .NET 10 SDK (or later)
- GitHub Copilot SDK 0.2.1 (stable)

## License

MIT
