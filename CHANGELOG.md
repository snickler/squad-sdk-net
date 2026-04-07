# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
- Skill security scanner — static analysis of skill markdown for embedded credentials, download-execute patterns, and privilege escalation (ports upstream `scripts/security-review.mjs`)
- Platform detection (OS, terminal, IDE)
- Import/export for portable squad configurations
- RemoteBridge with IAsyncDisposable for proper resource cleanup
- Full AOT / Native AOT compatibility
- Source-generated JSON serialization (4 contexts)
- Microsoft.Extensions.DependencyInjection integration
- SourceLink and symbol packages for source-level debugging
- Deterministic builds with TreatWarningsAsErrors
- Comprehensive XML documentation on all public APIs
- 582+ unit tests across 21+ test classes

### Infrastructure
- Multi-OS CI matrix (Ubuntu + Windows) with code coverage
- Tag-driven release workflow with SemVer validation
- Build provenance attestation for supply-chain security
- CodeQL security scanning (weekly + on PR)
- Dependency review on pull requests
- CODEOWNERS for required review enforcement
- NuGet package metadata (ready for publishing)
- Community files (CONTRIBUTING, SECURITY, issue/PR templates)
