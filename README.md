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
│   └── Squad.SDK.NET/          # Core SDK library
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
│   └── Squad.SDK.NET.Tests/    # Unit tests (433+ tests)
├── Squad.SDK.NET.slnx          # Solution file
├── Directory.Build.props       # Shared build properties
├── Directory.Packages.props    # Central package management
└── global.json                 # SDK version pinning
```

## Requirements

- .NET 10 SDK (or later)
- GitHub Copilot SDK

## License

MIT

---
