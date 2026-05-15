# Remaining Tasks from 2026-05-15 Upstream Sync

## Overview

The initial sync ported resolution caching, parallel utilities, and updated the sync state. The following tasks remain to complete the sync of upstream commits 12adffe → 754de09.

## Execution Status (2026-05-15)

- [x] Task 1: Apply parallel charter discovery
- [x] Task 2: Evaluate async scheduler improvements (no .NET scheduler equivalent exists in `src/Squad.SDK.NET/Runtime`)
- [x] Task 3: Add tests for resolution caching
- [x] Task 4: Add tests for parallel utilities
- [x] Task 5: Update documentation for current branch model
- [x] Task 6: Resolve Nerdbank.MessagePack vulnerability (NU1903)

---

## Task 1: Apply Parallel Charter Discovery

**Owner:** Holden (Lead .NET Architect)
**Priority:** High
**Upstream Commit:** c2698c8
**Estimated Effort:** 2-3 hours

### Context

Upstream added parallel charter discovery with bounded concurrency (5 concurrent operations) to improve agent loading performance. We have the `ParallelHelpers.MapWithLimitAsync()` utility ready to use.

### Implementation Details

**Upstream change (packages/squad-sdk/src/agents/index.ts):**
```typescript
// Before: Sequential loading
for (const agentDir of agentDirs) {
  const charter = await loadCharter(agentDir);
  charters.push(charter);
}

// After: Parallel loading with limit of 5
const charters = await mapWithLimit(agentDirs, 5, async (dir) => {
  return await loadCharter(dir);
});
```

**Target files in Squad.SDK.NET:**
- `src/Squad.SDK.NET/Config/ConfigLoader.cs` - Charter discovery and loading
- `src/Squad.SDK.NET/Agents/CharterCompiler.cs` - Charter compilation
- Any code that loads multiple charters sequentially

### Acceptance Criteria

- [ ] Locate all places where charters/agents are loaded sequentially
- [ ] Replace sequential loading with `ParallelHelpers.MapWithLimitAsync(items, 5, LoadAsync)`
- [ ] Ensure concurrency limit is 5 (matching upstream)
- [ ] Verify no breaking changes to public API
- [ ] Confirm builds successfully (ignoring pre-existing Nerdbank.MessagePack warning)

### References

- Upstream commit: https://github.com/bradygaster/squad/commit/c2698c8
- New utility: `src/Squad.SDK.NET/Runtime/ParallelHelpers.cs`
- Sync notes: `.sync/sync-notes-2026-05-15.md` (lines 64-67)

---

## Task 2: Apply Async Scheduler Improvements

**Owner:** Holden (Lead .NET Architect)
**Priority:** High
**Upstream Commit:** 39633fe
**Estimated Effort:** 3-4 hours

### Context

Upstream improved the scheduler to use non-blocking script task execution. This allows the scheduler to process multiple tasks concurrently without blocking the event loop.

### Investigation Required

First, locate the equivalent of upstream's `src/runtime/scheduler.ts` in Squad.SDK.NET:
- Check `src/Squad.SDK.NET/Runtime/` directory
- Look for scheduler, task execution, or script runner code
- Determine if we have an equivalent concept to their script task execution

### Upstream Changes Summary

From commit 39633fe:
- Changed from synchronous task execution to async/await patterns
- Tasks no longer block the scheduler while executing
- Multiple tasks can run concurrently (up to a limit)

### Acceptance Criteria

- [ ] Locate scheduler/script execution code in Squad.SDK.NET
- [ ] Analyze upstream changes in `packages/squad-sdk/src/runtime/scheduler.ts`
- [ ] Determine applicable changes for .NET implementation
- [ ] Apply async improvements (if applicable)
- [ ] Update any blocking calls to use `await` with `ConfigureAwait(false)`
- [ ] Confirm builds successfully

### References

- Upstream commit: https://github.com/bradygaster/squad/commit/39633fe
- Upstream file: `packages/squad-sdk/src/runtime/scheduler.ts` (+69 lines)
- Sync notes: `.sync/sync-notes-2026-05-15.md` (lines 69-71)

---

## Task 3: Add Tests for Resolution Caching

**Owner:** Drummer (Tester & QA)
**Priority:** High
**Estimated Effort:** 2-3 hours

### Context

We added resolution caching to `SquadResolver.cs` with TTL-based expiration and explicit invalidation. This needs comprehensive test coverage.

### Test Scenarios Required

1. **Cache Hit/Miss**
   - First call: cache miss, performs filesystem walk
   - Second call with same path: cache hit, no filesystem walk
   - Different paths: separate cache entries

2. **TTL Expiration**
   - Cache entry expires after 5 seconds
   - Expired entry triggers new filesystem walk
   - Fresh entry is re-cached

3. **Explicit Invalidation**
   - `ClearResolveSquadCache()` clears all cached entries
   - Next call after clear: cache miss
   - Works even with non-expired entries

4. **Escape Hatch**
   - `SQUAD_NO_RESOLVE_CACHE=1` disables caching
   - All calls perform filesystem walk
   - No entries added to cache

5. **Thread Safety**
   - Multiple threads calling `ResolveSquad()` concurrently
   - Cache remains consistent
   - No race conditions

### Test Location

`tests/Squad.SDK.NET.Tests/Resolution/SquadResolverTests.cs`

### Acceptance Criteria

- [ ] Create `SquadResolverTests.cs` (if doesn't exist) or extend existing tests
- [ ] Test all 5 scenarios above
- [ ] Use xUnit `[Fact]` and `[Theory]` attributes
- [ ] Follow naming convention: `MethodName_Scenario_ExpectedResult`
- [ ] Verify tests pass (ignoring pre-existing build warnings)

### References

- Implementation: `src/Squad.SDK.NET/Resolution/SquadResolver.cs` (lines 16-86)
- Sync notes: `.sync/sync-notes-2026-05-15.md` (lines 73-77)

---

## Task 4: Add Tests for Parallel Utilities

**Owner:** Drummer (Tester & QA)
**Priority:** High
**Estimated Effort:** 2-3 hours

### Context

We created `ParallelHelpers.cs` with two methods:
- `MapWithLimitAsync<T,TResult>()` - bounded concurrency, fails fast
- `MapWithLimitSettledAsync<T,TResult>()` - bounded concurrency, captures failures

These need comprehensive test coverage.

### Test Scenarios Required

#### For `MapWithLimitAsync`:

1. **Basic Functionality**
   - Input order preserved in output
   - All items processed
   - Correct results returned

2. **Concurrency Limit**
   - At most N tasks run concurrently (test with limit=3, items=10)
   - Track concurrent execution count
   - Verify limit is not exceeded

3. **Error Propagation**
   - One task fails → entire operation fails
   - Exception propagates to caller
   - Subsequent tasks may or may not complete

4. **Edge Cases**
   - Empty input → empty output
   - Limit=1 → sequential execution
   - Limit > items.Count → all run in parallel
   - Invalid limit (0, -1) → ArgumentException

#### For `MapWithLimitSettledAsync`:

1. **Partial Failures**
   - Some tasks succeed, some fail
   - Successful results captured
   - Failed results captured with exception
   - Input order preserved

2. **All Success / All Failure**
   - All tasks succeed → all results Success=true
   - All tasks fail → all results Success=false

3. **Result Inspection**
   - Success=true → Value is set, Exception is null
   - Success=false → Exception is set, Value is default

### Test Location

`tests/Squad.SDK.NET.Tests/Runtime/ParallelHelpersTests.cs`

### Acceptance Criteria

- [ ] Create `ParallelHelpersTests.cs`
- [ ] Test all scenarios for both methods
- [ ] Use async xUnit patterns (`async Task`)
- [ ] Verify concurrency limits (may need helper to track concurrent execution)
- [ ] Follow naming convention: `MethodName_Scenario_ExpectedResult`
- [ ] Verify tests pass

### References

- Implementation: `src/Squad.SDK.NET/Runtime/ParallelHelpers.cs`
- Sync notes: `.sync/sync-notes-2026-05-15.md` (lines 79-81)

---

## Task 5: Update Documentation for 2-Branch Model

**Owner:** Dawes (Technical Writer)
**Priority:** Medium
**Estimated Effort:** 1-2 hours

### Context

Upstream eliminated the `insider` branch in commit 987ac9b. They now use a 2-branch model (main + dev) with npm tags for insider builds. Our documentation may reference the old 3-branch model.

### Files to Review

Search for references to:
- "insider branch"
- "three branches" or "3 branches"
- Old sync workflow documentation
- Any upstream tracking documentation

### Changes Required

1. **Update sync documentation** (if exists)
   - Document current 2-branch model (main, dev)
   - Explain that insider builds are published via npm tags, not a separate branch
   - Update `.sync/upstream-state.json` schema documentation

2. **Update CONTRIBUTING.md** (if references branches)
   - Remove insider branch references
   - Update branch strategy section

3. **Update README.md** (if references upstream)
   - Clarify upstream tracking model

### Acceptance Criteria

- [ ] Search codebase for "insider" references
- [ ] Review all markdown files in repo root and `.sync/` directory
- [ ] Update any outdated branch model documentation
- [ ] Ensure `.sync/upstream-state.json` schema is documented correctly
- [ ] Commit with message: `docs: update to reflect upstream 2-branch model`

### References

- Upstream commit: https://github.com/bradygaster/squad/commit/987ac9b
- Updated sync state: `.sync/upstream-state.json`
- Sync notes: `.sync/sync-notes-2026-05-15.md` (lines 30-38, 85-87)

---

## Task 6: Resolve Nerdbank.MessagePack Vulnerability

**Owner:** Ashford (DevOps Engineer)
**Priority:** Medium
**Estimated Effort:** 1-2 hours

### Context

Builds are currently failing with:
```
error NU1903: Warning As Error: Package 'Nerdbank.MessagePack' 1.0.2 has a known high severity vulnerability, https://github.com/advisories/GHSA-2cwq-pwfr-wcw3
```

This is a pre-existing issue, not introduced by this sync, but it's blocking builds.

### Investigation Required

1. Check `Directory.Packages.props` for Nerdbank.MessagePack version
2. Review the vulnerability advisory: https://github.com/advisories/GHSA-2cwq-pwfr-wcw3
3. Determine if GitHub.Copilot.SDK depends on this package (transitive dependency)
4. Check if a newer version fixes the vulnerability

### Resolution Options

1. **Upgrade Nerdbank.MessagePack** (if available)
   - Check for version that fixes GHSA-2cwq-pwfr-wcw3
   - Test that upgrade doesn't break Copilot SDK compatibility

2. **Override transitive dependency** (if it's from Copilot SDK)
   - Add explicit package reference with safe version
   - Document why override is needed

3. **Temporary workaround** (last resort)
   - Suppress NU1903 for this specific package
   - Document technical debt
   - Create follow-up issue

### Acceptance Criteria

- [ ] Investigate vulnerability and dependency chain
- [ ] Determine safest resolution approach
- [ ] Apply fix
- [ ] Verify `dotnet build Squad.SDK.NET.slnx -c Release` succeeds
- [ ] Document decision in `.squad/decisions/inbox/ashford-messagepack-fix.md`
- [ ] Commit with message: `fix: resolve Nerdbank.MessagePack vulnerability (NU1903)`

### References

- Vulnerability: https://github.com/advisories/GHSA-2cwq-pwfr-wcw3
- Sync notes: `.sync/sync-notes-2026-05-15.md` (lines 89-91)

---

## Coordination Notes

### Parallel Execution

Tasks can be executed in parallel:
- **Holden's tasks (1, 2)** can run in parallel (different files)
- **Drummer's tasks (3, 4)** can run in parallel (different test files)
- **Dawes's task (5)** can run any time (documentation only)
- **Ashford's task (6)** can run any time (blocks builds but doesn't block code work)

### Dependencies

- Tasks 3 and 4 (tests) don't depend on tasks 1 and 2 (implementation) — they test the already-completed work
- Task 5 (documentation) is independent
- Task 6 (vulnerability) should be done soon to unblock CI, but doesn't block local development

### Success Criteria

The sync is complete when:
- All 6 tasks are marked complete
- All tests pass
- Build succeeds without errors (only after task 6)
- Documentation is up to date

---

## How to Use This File

### For Squad Coordination

Each task can be assigned to the appropriate agent via the Squad coordination system. The task owner is specified in each section.

### For Manual Execution

If executing tasks manually, follow the task order:
1. Tasks 1, 2 (Holden) - Implementation work
2. Tasks 3, 4 (Drummer) - Test coverage
3. Task 5 (Dawes) - Documentation
4. Task 6 (Ashford) - Dependency fix

### For Issue Tracking

Each task can be converted to a GitHub issue with the `squad` label and assigned to the appropriate team member via `squad:{agent}` label.
