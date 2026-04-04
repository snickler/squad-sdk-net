namespace Squad.SDK.NET.Roles;

public sealed record BaseRole
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public RoleCategory Category { get; init; } = RoleCategory.Engineering;
    public string? DefaultModel { get; init; }
    public IReadOnlyList<string> DefaultTools { get; init; } = [];
    public IReadOnlyList<string> Expertise { get; init; } = [];
    public string? PromptTemplate { get; init; }
}

public enum RoleCategory
{
    Engineering,
    Testing,
    Documentation,
    Architecture,
    DevRel,
    Design,
    Security,
    Ops
}
