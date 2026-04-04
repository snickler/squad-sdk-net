namespace Squad.SDK.NET.Config;

/// <summary>Configuration for agent casting, controlling which agents are available and how overflow is handled.</summary>
public sealed record CastingConfig
{
    /// <summary>Gets the list of allowlisted universes from which agents may be cast.</summary>
    public IReadOnlyList<string> AllowlistUniverses { get; init; } = [];

    /// <summary>Gets the strategy used when the agent pool reaches capacity.</summary>
    public OverflowStrategy OverflowStrategy { get; init; } = OverflowStrategy.Rotate;

    /// <summary>Gets the maximum number of agents that can be active simultaneously.</summary>
    public int? Capacity { get; init; }
}

/// <summary>Specifies the strategy used when the agent pool reaches capacity.</summary>
public enum OverflowStrategy
{
    /// <summary>Rotate out the least-recently-used agent.</summary>
    Rotate,

    /// <summary>Queue the request until capacity is available.</summary>
    Queue,

    /// <summary>Reject the request immediately.</summary>
    Reject
}
