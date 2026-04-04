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
6. **Submit a PR** against `main`

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

## Code of Conduct

Be respectful and constructive. We're all here to build something great together.

## Questions?

Open a [discussion](https://github.com/snickler/squad-sdk-net/issues) or reach out to [@snickler](https://github.com/snickler).

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
