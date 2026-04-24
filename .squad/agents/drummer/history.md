## Learnings

### Upstream sync: bradygaster/squad dev (2026-04-24)
- Analyzed 12 commits from upstream dev (ec19f5e..ec07f38) — no direct .NET SDK changes
- All commits were JavaScript/CLI/docs: monorepo subfolder support, state backends documentation, CI cleanup, test pattern improvements
- Cross-platform test isolation pattern: use `os.tmpdir()` for temp dirs (already done in our SDK per prior work)
- SDK build verified: release build passes, all 690 tests pass, 0 warnings
- Added `nuget.config` with package source mapping for GitHub.Copilot.SDK (required for enterprise environments)
- Updated `.sync/upstream-state.json` to lastPortedSha: `ec07f3874126666a5753212c3af92b7054b270c6` and lastChecked timestamp
- Committed as `061e213b` referencing issue #47

### Skyflow → Squad rename in tests (2025)
- Replaced 4 `skyflow-` temp dir prefixes with `squad-` in `StorageStateResolutionTests.cs` (lines 158, 256, 807, 828)
- Replaced 4 `"skyflow"` service name strings with `"squad-sdk"` in `ConfigAndBuilderParityTests.cs` (lines 427, 840, 849, 998)
- Used `"squad-sdk"` rather than `"squad"` for telemetry service names to be more descriptive and avoid collision with the project namespace
- Post-change verification: 0 Skyflow matches in `tests/`, build 0 warnings 0 errors, all 433 tests passed, no AOT warnings

### AOT / ConfigJsonContext test coverage (2025)
- Added `ConfigLoaderTests.cs` with 16 new tests covering `ConfigLoader.LoadAsync`, `LoadSync`, and the AOT source-gen JSON context
- Test categories: LoadAsync (7 tests), LoadSync (4 tests), round-trip serialization (2 tests), string enum deserialization (1 test), validate integration (2 tests)
- `ConfigJsonContext` is `internal`, so tested indirectly through the public `ConfigLoader` API — round-trip and enum tests confirm the source-gen context options (`PropertyNameCaseInsensitive`, `UseStringEnumConverter`) are working
- All tests use temp files with `IDisposable` cleanup
- No existing tests needed updating — the old reflection-based `JsonSerializerOptions` was only used inside `ConfigLoader`, and existing `ConfigValidationTests` only test `Validate()` with in-memory configs
- Post-change verification: build 0 warnings 0 errors, all 449 tests passed (was 433)

### Coverage tests for Holden's recent changes (2025)
- Added 8 new tests across 2 files covering 3 API changes from Holden
- **CastingEngine Queue overflow**: added `Cast_WithCapacityQueue_ThrowsNotSupportedException` in `AdvancedModulesTests.cs` following the Reject/Rotate test pattern at line ~510
- **RCServerEvent.Data as JsonElement?**: added 2 tests in `RemoteProtocolTests` class: `RCServerEvent_DataAsJsonElement_WorksCorrectly` (parsing JSON, extracting properties) and `RCServerEvent_DataNull_WorksCorrectly` (null data case)
- **MultiSquadManager invalid filename chars**: added 2 `[Theory]` tests with 7 `[InlineData]` entries each in `StorageStateResolutionTests.cs` after the path traversal tests at line ~941: `CreateSquad_InvalidFileNameChars_ThrowsArgumentException` and `DeleteSquad_InvalidFileNameChars_ThrowsArgumentException`
- Added `using System.Text.Json;` to `AdvancedModulesTests.cs` for `JsonDocument` and `JsonElement` support
- Post-change verification: all 457 tests passed (was 449), 0 warnings, 0 errors
