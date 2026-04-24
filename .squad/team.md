# Squad Team

> Squad.SDK.NET — A .NET SDK for multi-agent orchestration

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. |

## Members

### 👑 Team Leads

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Holden | Lead .NET Architect | [charter](.squad/agents/holden/charter.md) | active |

### 🏛️ Directors

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Fred | Director of Engineering | [charter](.squad/agents/fred/charter.md) | active |
| Ashford | Director of Platform & DevOps | [charter](.squad/agents/ashford/charter.md) | active |
| Dawes | Director of Developer Relations | [charter](.squad/agents/dawes/charter.md) | active |

### ⚙️ Individual Contributors

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Drummer | Tester & QA | [charter](.squad/agents/drummer/charter.md) | active |

### Silent / Monitor

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Scribe | Session Logger | [charter](.squad/agents/scribe/charter.md) | active |
| Ralph | Work Monitor | [charter](.squad/agents/ralph/charter.md) | active |


## Coding Agent

<!-- copilot-auto-assign: true -->

| Name | Role | Charter | Status |
|------|------|---------|--------|
| @copilot | Coding Agent | — | 🤖 Coding Agent |

### Capabilities

**🟢 Good fit — auto-route when enabled:**
- Bug fixes with clear reproduction steps
- Test coverage (adding missing tests, fixing flaky tests)
- Lint/format fixes and code style cleanup
- Dependency updates and version bumps
- Small isolated features with clear specs
- Boilerplate/scaffolding generation
- Documentation fixes and README updates

**🟡 Needs review — route to @copilot but flag for squad member PR review:**
- Medium features with clear specs and acceptance criteria
- Refactoring with existing test coverage
- API endpoint additions following established patterns
- Migration scripts with well-defined schemas

**🔴 Not suitable — route to squad member instead:**
- Architecture decisions and system design
- Multi-system integration requiring coordination
- Ambiguous requirements needing clarification
- Security-critical changes (auth, encryption, access control)
- Performance-critical paths requiring benchmarking
- Changes requiring cross-team discussion

## Project Context

- **Project:** Squad.SDK.NET — A .NET SDK for building multi-agent orchestration systems
- **Owner:** Jeremy Sinclair
- **Stack:** .NET 10, C#, xUnit, Central Package Management, GitHub Copilot SDK
- **Structure:** `src/Squad.SDK.NET/`, `tests/Squad.SDK.NET.Tests/`
- **Universe:** The Expanse
- **Created:** 2026-04-04
- **Origin:** Squad.SDK.NET
