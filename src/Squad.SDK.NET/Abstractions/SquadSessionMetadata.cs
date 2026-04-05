namespace Squad.SDK.NET.Abstractions;

/// <summary>
/// Read-only metadata describing a persisted session.
/// </summary>
public sealed record SquadSessionMetadata
{
    /// <summary>The unique identifier of the session.</summary>
    public required string SessionId { get; init; }

    /// <summary>When the session was created, if known.</summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>When the session was last active, if known.</summary>
    public DateTimeOffset? LastActiveAt { get; init; }

    /// <summary>The name of the agent associated with this session, if any.</summary>
    public string? AgentName { get; init; }
}
