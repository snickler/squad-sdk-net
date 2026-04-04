## Learnings

### AOT Readiness Audit (2025)

- **ConfigLoader.cs** was the sole AOT violation — used reflection-based `JsonSerializerOptions` with `JsonStringEnumConverter`. Replaced with dedicated `ConfigJsonContext` source-gen context.
- Created `src/Squad.SDK.NET/Config/ConfigJsonContext.cs` — separate context from `SharingJsonContext` to keep config-specific options (`PropertyNameCaseInsensitive`, `UseStringEnumConverter`) isolated from sharing serialization behavior.
- Added `<IsAotCompatible>true</IsAotCompatible>` to `Squad.SDK.NET.csproj` to enable build-time AOT analysis.
- `SquadState` and `TypedCollection<T>` already used `JsonTypeInfo<T>` — no changes needed.
- All DI registrations in `ServiceCollectionExtensions.cs` use concrete factory delegates — AOT safe.
- No reflection, no `dynamic`, no expression compilation anywhere in the SDK.

### Package Update: GitHub.Copilot.SDK 0.2.1-preview.1 → 0.2.1 (stable)

- Updated `GitHub.Copilot.SDK` from `0.2.1-preview.1` to stable `0.2.1`. No API surface changes detected — `CopilotClient`, `CopilotSession`, `CopilotClientOptions`, `ConnectionState`, and all other imported types remain identical.
- Also bumped: `Microsoft.Extensions.Logging` (10.0.0→10.0.5), `Microsoft.Extensions.Logging.Abstractions` (10.0.4→10.0.5), `Microsoft.NET.Test.Sdk` (17.12.0→18.3.0), `xunit` (2.9.2→2.9.3), `xunit.runner.visualstudio` (2.8.2→3.1.5), `coverlet.collector` (6.0.2→8.0.1).
- All 449 tests pass, zero warnings. The preview→stable transition was seamless with no breaking changes.

### Copilot Review Fixes: Batch 2 (2025)

- **CastingEngine.cs** — `OverflowStrategy.Queue` was silently proceeding after logging, creating casts beyond capacity. Now throws `NotSupportedException` since queueing isn't implemented. This is the correct fail-fast behavior.
- **RemoteProtocol.cs** — Changed `RCServerEvent.Data` from `object?` to `JsonElement?` for AOT safety. Added `RemoteJsonContext` source-gen context for serializing `List<RCAgent>`. Other payloads use `JsonDocument.Parse().RootElement.Clone()` for zero-reflection construction.
- **RemoteProtocol.cs** — Added `Pong`, `AgentsListed`, `Status` constants to `RemoteEvents`. Replaced all string literals in `RemoteBridge.cs` with these constants.
- **MultiSquadManager.cs** — Added `Path.GetInvalidFileNameChars()` validation in both `CreateSquad` and `DeleteSquad` to catch `:`, NUL, and other OS-invalid characters that passed the existing path separator checks.
- All 457 tests pass (8 new from recent work), zero AOT warnings.
