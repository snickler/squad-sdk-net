namespace Squad.SDK.NET.Skills;

public sealed record SkillDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<string> Triggers { get; init; } = [];
    public IReadOnlyList<string> AgentRoles { get; init; } = [];
    public required string Content { get; init; }
    public SkillConfidence Confidence { get; init; } = SkillConfidence.Medium;
}

public enum SkillConfidence { Low, Medium, High }

public sealed record SkillMatch
{
    public required SkillDefinition Skill { get; init; }
    public double Score { get; init; }
    public string? Reason { get; init; }
}
