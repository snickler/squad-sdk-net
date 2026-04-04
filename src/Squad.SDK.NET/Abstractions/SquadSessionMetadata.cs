namespace Squad.SDK.NET.Abstractions;

public sealed record SquadSessionMetadata
{
    public required string SessionId { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public DateTimeOffset? LastActiveAt { get; init; }
    public string? AgentName { get; init; }
}
