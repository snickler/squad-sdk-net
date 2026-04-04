using System.Text.Json.Serialization;
using Squad.SDK.NET.Config;
using Squad.SDK.NET.Coordinator;
using Squad.SDK.NET.Hooks;

namespace Squad.SDK.NET.Sharing;

[JsonSerializable(typeof(ExportedSquad))]
[JsonSerializable(typeof(ExportedAgent))]
[JsonSerializable(typeof(ImportResult))]
[JsonSerializable(typeof(SquadConfig))]
[JsonSerializable(typeof(TeamConfig))]
[JsonSerializable(typeof(AgentConfig))]
[JsonSerializable(typeof(RoutingConfig))]
[JsonSerializable(typeof(RoutingRule))]
[JsonSerializable(typeof(ModelSelectionConfig))]
[JsonSerializable(typeof(ModelPreference))]
[JsonSerializable(typeof(DefaultsConfig))]
[JsonSerializable(typeof(CeremonyConfig))]
[JsonSerializable(typeof(CastingConfig))]
[JsonSerializable(typeof(TelemetryConfig))]
[JsonSerializable(typeof(SkillConfig))]
[JsonSerializable(typeof(HooksDefinition))]
[JsonSerializable(typeof(BudgetConfig))]
[JsonSerializable(typeof(AgentCapability))]
[JsonSerializable(typeof(PolicyConfig))]
[JsonSerializable(typeof(ModelTier))]
[JsonSerializable(typeof(ResponseTier))]
[JsonSerializable(typeof(AgentStatus))]
[JsonSerializable(typeof(RoutingFallbackBehavior))]
[JsonSerializable(typeof(OverflowStrategy))]
[JsonSerializable(typeof(SkillConfidenceLevel))]
internal partial class SharingJsonContext : JsonSerializerContext
{
}
