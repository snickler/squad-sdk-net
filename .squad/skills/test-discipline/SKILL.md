---
name: test-discipline
description: API changes require test updates in the same commit
domain: testing
confidence: high
source: established for Squad.SDK.NET
---

## Rule

**Every API change must include corresponding test updates in the same commit.**

This means:
- New public methods → new test methods
- Changed method signatures → updated test assertions
- New models/records → tests for serialization and equality
- Bug fixes → regression test proving the fix

## Enforcement

1. Drummer (QA) has veto power over any PR without tests
2. The reviewer checks for test coverage before approving
3. CI must pass all tests before merge

## Exceptions

- Documentation-only changes don't need tests
- CI/CD pipeline changes don't need tests (but should be tested manually)
- `.squad/` configuration changes don't need tests

## Anti-Patterns

- ❌ "I'll add tests later" — no, add them now
- ❌ Tests that don't actually assert anything
- ❌ Tests that depend on external services or network
- ❌ Tests that are flaky or order-dependent
