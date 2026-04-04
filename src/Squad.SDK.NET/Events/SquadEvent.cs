namespace Squad.SDK.NET.Events;

public record SquadEvent
{
    public required SquadEventType Type { get; init; }
    public string? SessionId { get; init; }
    public string? AgentName { get; init; }
    public object? Payload { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
