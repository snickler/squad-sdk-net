## Learnings

### AOT Readiness Audit (2025)

- **ConfigLoader.cs** was the sole AOT violation — used reflection-based `JsonSerializerOptions` with `JsonStringEnumConverter`. Replaced with dedicated `ConfigJsonContext` source-gen context.
- Created `src/Squad.SDK.NET/Config/ConfigJsonContext.cs` — separate context from `SharingJsonContext` to keep config-specific options (`PropertyNameCaseInsensitive`, `UseStringEnumConverter`) isolated from sharing serialization behavior.
- Added `<IsAotCompatible>true</IsAotCompatible>` to `Squad.SDK.NET.csproj` to enable build-time AOT analysis.
- `SquadState` and `TypedCollection<T>` already used `JsonTypeInfo<T>` — no changes needed.
- All DI registrations in `ServiceCollectionExtensions.cs` use concrete factory delegates — AOT safe.
- No reflection, no `dynamic`, no expression compilation anywhere in the SDK.
