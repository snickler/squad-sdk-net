namespace Squad.SDK.NET.Config;

public sealed record SkillConfig
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Domain { get; init; }
    public SkillConfidenceLevel Confidence { get; init; } = SkillConfidenceLevel.Medium;
    public string? Source { get; init; }
    public string? Content { get; init; }
    public IReadOnlyList<string> Tools { get; init; } = [];
}

public enum SkillConfidenceLevel { Low, Medium, High }
