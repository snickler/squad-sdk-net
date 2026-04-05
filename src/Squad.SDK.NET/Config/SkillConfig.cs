namespace Squad.SDK.NET.Config;

/// <summary>Configuration for a skill that can be assigned to agents.</summary>
public sealed record SkillConfig
{
    /// <summary>Gets the unique name of the skill.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the human-readable description of the skill.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the domain this skill applies to.</summary>
    public string? Domain { get; init; }

    /// <summary>Gets the confidence level indicating how reliably the skill can be applied.</summary>
    public SkillConfidenceLevel Confidence { get; init; } = SkillConfidenceLevel.Medium;

    /// <summary>Gets the source or origin of the skill definition.</summary>
    public string? Source { get; init; }

    /// <summary>Gets the raw content or prompt text for the skill.</summary>
    public string? Content { get; init; }

    /// <summary>Gets the list of tools required by this skill.</summary>
    public IReadOnlyList<string> Tools { get; init; } = [];
}

/// <summary>Represents the confidence level of a skill.</summary>
public enum SkillConfidenceLevel
{
    /// <summary>Low confidence — the skill may produce unreliable results.</summary>
    Low,

    /// <summary>Medium confidence — the skill is generally reliable.</summary>
    Medium,

    /// <summary>High confidence — the skill is highly reliable.</summary>
    High
}
