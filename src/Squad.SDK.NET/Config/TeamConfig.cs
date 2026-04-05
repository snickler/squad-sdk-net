namespace Squad.SDK.NET.Config;

/// <summary>Configuration for the squad team, including its name and default model settings.</summary>
public sealed record TeamConfig
{
    /// <summary>Gets the name of the team.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional human-readable description of the team.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the default model identifier used by agents in this team.</summary>
    public string? DefaultModel { get; init; }

    /// <summary>Gets the default model tier for the team.</summary>
    public ModelTier DefaultTier { get; init; } = ModelTier.Standard;
}
