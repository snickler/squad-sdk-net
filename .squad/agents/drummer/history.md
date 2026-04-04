## Learnings

### Skyflow → Squad rename in tests (2025)
- Replaced 4 `skyflow-` temp dir prefixes with `squad-` in `StorageStateResolutionTests.cs` (lines 158, 256, 807, 828)
- Replaced 4 `"skyflow"` service name strings with `"squad-sdk"` in `ConfigAndBuilderParityTests.cs` (lines 427, 840, 849, 998)
- Used `"squad-sdk"` rather than `"squad"` for telemetry service names to be more descriptive and avoid collision with the project namespace
- Post-change verification: 0 Skyflow matches in `tests/`, build 0 warnings 0 errors, all 433 tests passed, no AOT warnings
