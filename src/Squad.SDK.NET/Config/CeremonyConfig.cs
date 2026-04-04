using Squad.SDK.NET.Hooks;

namespace Squad.SDK.NET.Config;

public sealed record CeremonyConfig
{
    public required string Name { get; init; }
    public string? Trigger { get; init; }
    public string? Schedule { get; init; }
    public IReadOnlyList<string> Participants { get; init; } = [];
    public IReadOnlyList<string> Agenda { get; init; } = [];
    public PolicyConfig? Hooks { get; init; }
}
