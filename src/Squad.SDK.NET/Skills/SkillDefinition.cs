namespace Squad.SDK.NET.Skills;

/// <summary>
/// Defines a skill that can be matched to tasks and assigned to agents.
/// </summary>
public sealed record SkillDefinition
{
    /// <summary>Gets the unique skill identifier.</summary>
    public required string Id { get; init; }
    /// <summary>Gets the human-readable skill name.</summary>
    public required string Name { get; init; }
    /// <summary>Gets the keywords that trigger this skill during matching.</summary>
    public IReadOnlyList<string> Triggers { get; init; } = [];
    /// <summary>Gets the agent roles this skill applies to.</summary>
    public IReadOnlyList<string> AgentRoles { get; init; } = [];
    /// <summary>Gets the skill content (typically loaded from a SKILL.md file).</summary>
    public required string Content { get; init; }
    /// <summary>Gets the confidence level used to boost or reduce match scores.</summary>
    public SkillConfidence Confidence { get; init; } = SkillConfidence.Medium;
}

/// <summary>Indicates the confidence level of a skill definition.</summary>
public enum SkillConfidence
{
    /// <summary>Low confidence.</summary>
    Low,

    /// <summary>Medium confidence.</summary>
    Medium,

    /// <summary>High confidence.</summary>
    High
}

/// <summary>
/// Represents a skill matched against a task, including a relevance score.
/// </summary>
public sealed record SkillMatch
{
    /// <summary>Gets the matched skill definition.</summary>
    public required SkillDefinition Skill { get; init; }
    /// <summary>Gets the match score between 0.0 and 1.0.</summary>
    public double Score { get; init; }
    /// <summary>Gets an optional human-readable reason for the match.</summary>
    public string? Reason { get; init; }
}
