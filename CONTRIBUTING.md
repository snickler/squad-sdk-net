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
2. **Create a branch** from `dev` (`git fetch origin dev && git checkout -b feature/your-feature origin/dev`)
3. **Make your changes** — follow the code style guidelines below
4. **Add or update tests** for any new functionality
5. **Ensure all tests pass** (`dotnet test`)
6. **Add a `.changeset` entry** when you change releaseable SDK behavior under `src/Squad.SDK.NET/`
7. **Create a draft PR** against `dev`
8. **Mark the PR ready for review** after CI passes and you're satisfied with the result

### Handoff: Contributor → Core Team

External contributors do not have write access, so the review-to-merge flow has a handoff point.

**Contributor side**

1. All required CI checks are green
2. The PR is no longer a draft
3. Copilot review suggestions are addressed manually in your fork
4. Any remaining blockers or tradeoffs are noted in the PR conversation

**Core team side**

1. Review the now-ready PR
2. Address any remaining repository-side follow-up
3. Merge into `dev`

> **Note:** Copilot review comments may suggest changes, but the repository-side quick actions can require write access. Apply the changes in your fork and push the updates normally.

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

## Testing Template Changes (End-to-End)

Changes to Squad coordinator/charter templates should be validated with a real session flow in addition to unit tests.

### Quick run

```bash
mkdir -p /tmp/squad-template-e2e && cd /tmp/squad-template-e2e
git init
echo "# Template E2E" > README.md
git add -A && git commit -m "init"

copilot --agent squad --allow-all-tools -p "Picard, decide on a testing framework and record the decision."
```

### Full workflow

Use the full checklist and evidence workflow in:

- `.copilot/skills/e2e-template-testing/SKILL.md`
- `.squad/templates/skills/e2e-template-testing/SKILL.md`
- `src/Squad.SDK.NET/Templates/skills/e2e-template-testing/SKILL.md`

## Versioning

This project follows [Semantic Versioning 2.0](https://semver.org/) with changeset-driven release prep on `dev`. See [VERSIONING.md](VERSIONING.md) for the full branching, versioning, and release policy.

### Creating a Release

1. Merge releaseable work into `dev`, with `.changeset/*.md` entries for SDK changes
2. Run the `promote.yml` workflow with **`dev-to-preview`**
3. Let `preview.yml` validate the staged release candidate
4. Run the `promote.yml` workflow with **`preview-to-main`**
5. The [release workflow](.github/workflows/release.yml) creates the tag, GitHub Release, and NuGet publication from `main`

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
