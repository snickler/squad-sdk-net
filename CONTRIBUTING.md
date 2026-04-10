# Contributing to Squad.SDK.NET

Thank you for your interest in contributing! This guide will help you get started.

## Reporting Bugs

Please use [GitHub Issues](https://github.com/snickler/squad-sdk-net/issues) to report bugs. Include:

- A clear, descriptive title
- Steps to reproduce the issue
- Expected vs. actual behavior
- .NET version, OS, and any relevant environment details

## Suggesting Features

Open a [GitHub Issue](https://github.com/snickler/squad-sdk-net/issues) with the **enhancement** label. Describe the problem you're solving and the solution you'd like to see.

## Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later

### Getting Started

```bash
git clone https://github.com/snickler/squad-sdk-net.git
cd squad-sdk-net
dotnet build
dotnet test
```

## Pull Request Process

1. **Fork** the repository
2. **Create a branch** from `main` (`git checkout -b feature/your-feature`)
3. **Make your changes** — follow the code style guidelines below
4. **Add or update tests** for any new functionality
5. **Ensure all tests pass** (`dotnet test`)
6. **Create a PR as a draft** — `gh pr create --draft --base main --repo snickler/squad-sdk-net --head {yourusername}:your-branch`
7. **Work on your changes** until CI passes and you're satisfied
8. **Mark as "Ready for review"** — this is the handoff signal to the core team (see below)

### Handoff: Contributor → Core Team

External contributors don't have write access, so the review-to-merge flow has a handoff point. Here's exactly what happens:

**Your side (contributor):**

1. ✅ All required CI checks are green (build, test)
2. ✅ PR is no longer a draft — mark as **"Ready for review"**
3. ✅ Copilot reviewer bot posts its review automatically (if configured)
4. ✅ Review Copilot's suggestions and manually apply any you agree with in your fork
5. ✅ Push updates to your branch to address feedback
6. ✅ If Copilot flags issues you can't resolve, note them in a PR comment

> **Note:** Copilot review suggestions appear as comments, but the "Commit suggestion" and "Fix with Copilot" buttons require repo write access and won't work for external contributors. Review the suggestions, apply them manually in your fork, and push your changes.

**Core team side (after you undraft):**

1. Look for CI-green, undrafted PRs from contributors
2. Address any remaining Copilot review issues (using "Fix with Copilot" or manual fixes)
3. Human review, resolve threads, and merge

**TL;DR:** Your job is done when the PR is undrafted, CI is green, and you've responded to suggestions. The core team takes it from there.

### PR Guidelines

- Keep PRs focused — one feature or fix per PR
- Write clear commit messages
- Update documentation if your change affects public APIs
- Ensure the build passes cleanly with no warnings

## Code Style

- Follow the [.editorconfig](.editorconfig) in the repository root
- Use file-scoped namespaces
- Use `PascalCase` for public members, `_camelCase` for private fields
- Add XML documentation to all public APIs

## Tests

- All new features must include unit tests
- All bug fixes should include a regression test
- Tests use xUnit — place them in the `tests/` directory

## Versioning

This project follows [Semantic Versioning 2.0](https://semver.org/). See [VERSIONING.md](VERSIONING.md) for the full versioning policy, pre-release conventions, and the release process.

### Creating a Release

1. Update `<Version>` in `src/Squad.SDK.NET/Squad.SDK.NET.csproj`
2. Update `CHANGELOG.md` with release notes
3. Merge to `main`
4. Tag the merge commit: `git tag v0.2.0 && git push origin v0.2.0`
5. The [release workflow](.github/workflows/release.yml) handles the rest

## Security

Please report vulnerabilities privately — see [SECURITY.md](SECURITY.md) for details.

All pull requests are scanned by:
- **CodeQL** — static analysis for security vulnerabilities
- **Dependency Review** — flags high-severity vulnerabilities and restrictive licenses

## Code of Conduct

Be respectful and constructive. We're all here to build something great together.

## Questions?

Open a [discussion](https://github.com/snickler/squad-sdk-net/issues) or reach out to [@snickler](https://github.com/snickler).

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
