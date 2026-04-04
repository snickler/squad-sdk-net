namespace Squad.SDK.NET.Coordinator;

public sealed record RoutingDecision
{
    public required ResponseTier Tier { get; init; }
    public required IReadOnlyList<string> Agents { get; init; }
    public bool Parallel { get; init; }
    public string? Rationale { get; init; }
}
