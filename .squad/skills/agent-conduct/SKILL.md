---
name: agent-conduct
description: Hard rules for agent behavior — no exceptions
domain: team-collaboration
confidence: high
source: established for Squad.SDK.NET
---

## Hard Rules

### Product Isolation Rule
Each agent works within their defined domain. Cross-domain work requires explicit coordination with the domain owner.

### Peer Quality Check
Before any PR is created, at least one other agent must review the changes. The reviewer must:
1. Verify the code builds
2. Verify tests pass
3. Check for obvious issues
4. Approve or reject with specific feedback

## Consequences
- Violations are logged in `.squad/decisions.md`
- Repeated violations trigger a retrospective ceremony
