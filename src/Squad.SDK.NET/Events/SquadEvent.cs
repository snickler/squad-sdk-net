namespace Squad.SDK.NET.Events;

/// <summary>
/// Represents an event raised by the Squad system, carrying a type, optional payload, and metadata.
/// </summary>
/// <seealso cref="SquadEventType"/>
public record SquadEvent
{
    /// <summary>Gets the type of event.</summary>
    public required SquadEventType Type { get; init; }

    /// <summary>Gets the session identifier associated with this event, if any.</summary>
    public string? SessionId { get; init; }

    /// <summary>Gets the agent name associated with this event, if any.</summary>
    public string? AgentName { get; init; }

    /// <summary>Gets the event-specific payload, or <see langword="null"/> if none.</summary>
    public object? Payload { get; init; }

    /// <summary>Gets the UTC timestamp when the event was created.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
