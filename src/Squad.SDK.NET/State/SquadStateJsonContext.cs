using System.Text.Json.Serialization;

namespace Squad.SDK.NET.State;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AgentEntity))]
[JsonSerializable(typeof(Decision))]
[JsonSerializable(typeof(HistoryEntry))]
[JsonSerializable(typeof(TeamMember))]
[JsonSerializable(typeof(Template))]
[JsonSerializable(typeof(LogEntry))]
[JsonSerializable(typeof(IReadOnlyList<AgentEntity>))]
[JsonSerializable(typeof(IReadOnlyList<Decision>))]
[JsonSerializable(typeof(IReadOnlyList<HistoryEntry>))]
[JsonSerializable(typeof(IReadOnlyList<LogEntry>))]
internal partial class SquadStateJsonContext : JsonSerializerContext
{
}
