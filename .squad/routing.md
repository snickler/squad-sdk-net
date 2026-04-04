# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| SDK architecture, API design, patterns | Holden | "What's the right abstraction for X?", major API changes |
| Engineering process, velocity, tech debt | Fred | Sprint planning, technical debt, project structure |
| CI/CD, build pipelines, NuGet packaging | Ashford | Build scripts, release pipeline, NuGet publishing |
| Documentation, README, API docs, examples | Dawes | Public API docs, changelogs, SDK getting-started guides |
| Testing, edge cases, test coverage | Drummer | Unit tests, integration tests, edge case discovery |
| Session logging | Scribe | Automatic — never needs routing |
| Work queue, backlog monitoring | Ralph | Active when "Ralph, go" is said |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Holden (Lead) |
| `squad:holden` | Architecture or API design issue | Holden |
| `squad:fred` | Engineering process issue | Fred |
| `squad:ashford` | DevOps or CI/CD issue | Ashford |
| `squad:dawes` | Docs or community issue | Dawes |
| `squad:drummer` | Test coverage or quality issue | Drummer |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, the **Lead** triages it.
2. When a `squad:{member}` label is applied, that member picks up the issue.
3. Members can reassign by removing their label and adding another member's label.

## MCP Tool Assignments

### NuGet Tools
| Tool | Purpose | Authorized Agents |
|------|---------|------------------|
| `nuget-update-package-to-version` | Update packages to specific versions | Holden, Ashford |
| `nuget-get-nuget-solver` | Fix vulnerable packages | Holden, Ashford |
| `nuget-get-package-context` | Package docs / README lookup | Holden, Ashford, Dawes |
| `nuget-get-latest-package-version` | Check latest available version | Holden, Ashford |

### Documentation Tools
| Tool | Purpose | Authorized Agents |
|------|---------|------------------|
| `context7-resolve-library-id` | Resolve library for doc lookup | Holden, Dawes |
| `context7-query-docs` | Query library documentation | Holden, Dawes |
| `microsoft-docs-mcp-microsoft_docs_search` | Search Microsoft/Azure docs | Holden, Dawes |

## Rules

1. **Eager by default** — spawn all agents who could usefully start work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`.
3. **Quick facts → coordinator answers directly.**
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester simultaneously.
