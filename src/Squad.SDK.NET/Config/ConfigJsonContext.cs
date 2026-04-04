using System.Text.Json.Serialization;

namespace Squad.SDK.NET.Config;

/// <summary>
/// Source-generated JSON serializer context for <see cref="SquadConfig"/> and related types.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(SquadConfig))]
internal partial class ConfigJsonContext : JsonSerializerContext
{
}
