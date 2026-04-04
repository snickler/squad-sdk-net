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
│   └── Squad.SDK.NET.Tests/    # Unit tests (474+ tests)
├── docs/
│   └── examples.md             # Comprehensive usage examples
├── .editorconfig               # Code style rules
├── .github/
│   ├── ISSUE_TEMPLATE/         # Bug report & feature request templates
│   ├── PULL_REQUEST_TEMPLATE.md
│   ├── workflows/
│   │   ├── ci.yml              # CI (multi-OS matrix, coverage, pack)
│   │   ├── codeql.yml          # CodeQL security scanning
│   │   ├── dependency-review.yml # Dependency vulnerability review
│   │   └── release.yml         # Tag-driven release + NuGet publish
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

- **[Usage Examples](docs/examples.md)** — 16-section cookbook with copy-paste-ready C# examples covering the Builder API, routing, events, hooks, cost tracking, sessions, skills, casting, import/export, and more.
- **API Documentation** — All public types, methods, and properties include comprehensive XML doc comments for IntelliSense and generated API reference.
- **[Package README](src/Squad.SDK.NET/README.md)** — Detailed feature overview, architecture diagram, and quick start guide.
- **[Changelog](CHANGELOG.md)** — Release notes following [Keep a Changelog](https://keepachangelog.com/) format.

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
