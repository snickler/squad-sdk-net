using Squad.SDK.NET.Coordinator;

namespace Squad.SDK.NET.Config;

/// <summary>Configuration for routing work items to agents based on work type.</summary>
public sealed record RoutingConfig
{
    /// <summary>Gets the ordered list of routing rules.</summary>
    public IReadOnlyList<RoutingRule> Rules { get; init; } = [];

    /// <summary>Gets the name of the default agent used when no rule matches.</summary>
    public string? DefaultAgent { get; init; }

    /// <summary>Gets the fallback behavior when no routing rule matches a work item.</summary>
    public RoutingFallbackBehavior FallbackBehavior { get; init; } = RoutingFallbackBehavior.Coordinator;
}

/// <summary>A single routing rule that maps a work type to one or more agents.</summary>
public sealed record RoutingRule
{
    /// <summary>Gets the work type this rule matches.</summary>
    public required string WorkType { get; init; }

    /// <summary>Gets the list of agent names that can handle this work type.</summary>
    public required IReadOnlyList<string> Agents { get; init; }

    /// <summary>Gets the optional response tier override for this rule.</summary>
    public ResponseTier? Tier { get; init; }

    /// <summary>Gets the priority of this rule; lower values are evaluated first.</summary>
    public int Priority { get; init; }
}

/// <summary>Specifies how the system behaves when no routing rule matches.</summary>
public enum RoutingFallbackBehavior
{
    /// <summary>Ask the user to choose an agent.</summary>
    Ask,

    /// <summary>Route to the configured default agent.</summary>
    DefaultAgent,

    /// <summary>Let the coordinator decide automatically.</summary>
    Coordinator
}
