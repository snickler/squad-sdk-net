using Squad.SDK.NET.Hooks;

namespace Squad.SDK.NET.Config;

/// <summary>Configuration for a squad ceremony, such as a standup or retrospective.</summary>
public sealed record CeremonyConfig
{
    /// <summary>Gets the name of the ceremony.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the event or condition that triggers the ceremony.</summary>
    public string? Trigger { get; init; }

    /// <summary>Gets the cron-style schedule expression for recurring ceremonies.</summary>
    public string? Schedule { get; init; }

    /// <summary>Gets the list of agent names that participate in this ceremony.</summary>
    public IReadOnlyList<string> Participants { get; init; } = [];

    /// <summary>Gets the agenda items for the ceremony.</summary>
    public IReadOnlyList<string> Agenda { get; init; } = [];

    /// <summary>Gets the optional policy hooks applied during this ceremony.</summary>
    public PolicyConfig? Hooks { get; init; }
}
