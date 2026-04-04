using System.Text.Json.Serialization;

namespace Squad.SDK.NET.Config;

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(SquadConfig))]
internal partial class ConfigJsonContext : JsonSerializerContext
{
}
