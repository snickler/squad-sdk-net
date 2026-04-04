using Squad.SDK.NET.Coordinator;

namespace Squad.SDK.NET.Config;

public sealed record RoutingConfig
{
    public IReadOnlyList<RoutingRule> Rules { get; init; } = [];
    public string? DefaultAgent { get; init; }
    public RoutingFallbackBehavior FallbackBehavior { get; init; } = RoutingFallbackBehavior.Coordinator;
}

public sealed record RoutingRule
{
    public required string WorkType { get; init; }
    public required IReadOnlyList<string> Agents { get; init; }
    public ResponseTier? Tier { get; init; }
    public int Priority { get; init; }
}

public enum RoutingFallbackBehavior { Ask, DefaultAgent, Coordinator }
