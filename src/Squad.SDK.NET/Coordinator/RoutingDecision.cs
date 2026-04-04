namespace Squad.SDK.NET.Coordinator;

/// <summary>
/// Represents the outcome of the coordinator's message routing, identifying which agents should handle the request.
/// </summary>
/// <seealso cref="Coordinator"/>
public sealed record RoutingDecision
{
    /// <summary>Gets the response quality tier for this routing decision.</summary>
    public required ResponseTier Tier { get; init; }

    /// <summary>Gets the names of the agents selected to handle the request.</summary>
    public required IReadOnlyList<string> Agents { get; init; }

    /// <summary>Gets a value indicating whether the selected agents should execute in parallel.</summary>
    public bool Parallel { get; init; }

    /// <summary>Gets an optional human-readable explanation of why this routing was chosen.</summary>
    public string? Rationale { get; init; }
}
