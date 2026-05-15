# Upstream Sync Notes — 2026-05-15

## Summary

Ported key changes from upstream bradygaster/squad `dev` branch covering 19 commits (12adffe → 754de09).

## What Was Ported

### 1. Resolution Caching (commit f36ea80)
**File:** `src/Squad.SDK.NET/Resolution/SquadResolver.cs`

Added in-process memoization for `.squad` directory lookups to avoid repeated filesystem walks:
- `ConcurrentDictionary` cache with 5-second TTL
- `ClearResolveSquadCache()` public method for explicit invalidation
- `SQUAD_NO_RESOLVE_CACHE=1` environment variable to disable caching
- Cache key: absolute path via `Path.GetFullPath()`

**Rationale:** `ResolveSquad()` walks from startDir toward root doing 2–3 syscalls per level. It's called multiple times per operation. Caching reduces redundant filesystem traversal.

### 2. Parallel Execution Utilities (commits c2698c8, 39633fe)
**File:** `src/Squad.SDK.NET/Runtime/ParallelHelpers.cs`

Created bounded-concurrency helpers for fan-out async work:
- `MapWithLimitAsync<T,TResult>()` - parallel map with max concurrency limit, results in input order
- `MapWithLimitSettledAsync<T,TResult>()` - same but captures individual failures instead of aborting
- `SettledResult<T>` record for success/failure discrimination

**Rationale:** Upstream added `map-with-limit.ts` utility for parallel charter discovery and other bounded fan-out operations. This is the C# equivalent.

### 3. Breaking Change: Insider Branch Elimination (commit 987ac9b)
**File:** `.sync/upstream-state.json`

Removed `insider` branch tracking. Upstream consolidated from 3 branches (main/dev/insider) to 2 branches (main/dev):
- Deleted `insider` object from sync state
- Updated `dev.lastPortedSha` to `754de0988f38120d81feac43e3f1db7330188979`
- Updated `dev.lastChecked` to `2026-05-15T18:24:00.000Z`

Upstream now publishes insider builds via npm tags (@bradygaster/squad-cli@insider) from the dev branch, eliminating the need for a separate insider branch.

## What Was NOT Ported (Upstream-Specific)

The following upstream changes are **CLI-specific** or **Node.js-specific** and don't apply to Squad.SDK.NET (a library, not a CLI):

1. **Benchmark infrastructure** (commits d757437, a383714)
   - `scripts/measure-cold-start.mjs`, `scripts/run-benchmarks.mjs`
   - `test/bench-runner.test.ts`
   - Reason: Node.js-specific benchmark runners; .NET has BenchmarkDotNet if needed

2. **CLI commands** (upstream has `squad sync`, `squad link`, etc.)
   - Reason: Squad.SDK.NET is a library consumed by applications, not a CLI tool

3. **Dependency cleanup** (commit 0f428d8)
   - Stopped committing `.tgz` tarballs, dropped redundant deps
   - Reason: Different package management model (NuGet vs npm)

4. **Workflow/CI changes** (commit 987ac9b)
   - Deleted `squad-insider-release.yml`, modified `squad-insider-publish.yml`
   - Reason: Squad.SDK.NET has its own CI workflows

## What Remains to Do

### High Priority

1. **Apply parallel charter discovery** (commit c2698c8)
   - Upstream: `src/agents/index.ts` — parallel charter loading with `mapWithLimit(..., 5, ...)`
   - Target: `src/Squad.SDK.NET/Agents/AgentSessionManager.cs` or charter loading logic
   - Use new `ParallelHelpers.MapWithLimitAsync()` utility

2. **Apply async scheduler improvements** (commit 39633fe)
   - Upstream: `src/runtime/scheduler.ts` — non-blocking script task execution
   - Target: TBD — locate scheduler/script execution in Squad.SDK.NET runtime

3. **Add tests for resolution caching**
   - Test cache hits/misses
   - Test TTL expiration
   - Test `SQUAD_NO_RESOLVE_CACHE=1` escape hatch
   - Test `ClearResolveSquadCache()` invalidation

4. **Add tests for parallel utilities**
   - Test `MapWithLimitAsync` ordering, concurrency limits, error propagation
   - Test `MapWithLimitSettledAsync` partial failure handling

### Medium Priority

5. **Update documentation**
   - Update any references to the insider branch (if any exist)
   - Document the 2-branch sync model

6. **Resolve Nerdbank.MessagePack vulnerability**
   - Current blocker: `error NU1903` for Nerdbank.MessagePack 1.0.2
   - Investigate upgrade path or alternative

### Low Priority

7. **Review remaining 19 commits for additional changes**
   - Check if any other SDK-relevant logic was missed
   - Review test changes for patterns we should adopt

## Upstream Commit Log (12adffe → 754de09)

```
754de09 Merge pull request #1114 from spboyer/perf/scheduler-async-exec
61824d0 Merge pull request #1113 from spboyer/perf/charter-discovery-parallel
95f09e7 Merge pull request #1112 from spboyer/perf/resolution-cache
a383714 Merge pull request #1111 from spboyer/feat/bench-runner
233964f Merge pull request #1110 from spboyer/chore/dep-hygiene
87ac974 fix(tests): clear resolution memoize cache in state-backend and dual-root-resolver hooks
892bd4f chore: add changeset for perf(scheduler) async exec PR
29cedd0 chore: add changeset for perf(charter) parallel discovery PR
10f5036 chore: add changeset for perf(resolution) memoize PR
39633fe perf(scheduler): non-blocking script task execution
c2698c8 perf(agents): parallel charter discovery with bounded concurrency
f36ea80 perf(resolution): memoize squad-dir lookups; dedupe squads.json reads
d757437 feat(bench): add npm run bench + bench:cold-start runners
0f428d8 chore(deps): drop redundant deps, stop committing tarballs, cache docs CI
d598857 Merge pull request #1059 from anchapin/alex/local-dev-setup-fix
60ae4ed fix: pin SDK dependency to insider version during publish (#1098)
c7d7382 fix: auto-generate insider prerelease versions + bump to 0.9.6 (#1097)
987ac9b chore: eliminate insider branch — consolidate to dev-based npm tag (#1095)
4658b03 docs: update local development setup documentation
```

## Files Changed (Upstream Diff)

Total: 38 files changed, 1,642 insertions(+), 226 deletions(-)

Key SDK changes:
- `packages/squad-sdk/src/resolution.ts` (+101 lines) — resolution cache
- `packages/squad-sdk/src/runtime/scheduler.ts` (+69 lines) — async execution
- `packages/squad-sdk/src/utils/map-with-limit.ts` (+91 lines) — new utility
- `packages/squad-sdk/src/agents/index.ts` (+67 lines, -3 deletions) — parallel charter loading
- `packages/squad-sdk/src/config/agent-source.ts` (+97 lines, -3 deletions) — parallel agent source resolution

## Next Steps

1. Locate charter loading logic in Squad.SDK.NET
2. Apply parallel loading with `ParallelHelpers.MapWithLimitAsync(agents, 5, LoadCharterAsync)`
3. Locate script/scheduler execution logic
4. Apply async improvements from upstream/dev@39633fe
5. Write tests for caching and parallel utilities
6. Verify full build passes after Nerdbank.MessagePack issue is resolved
