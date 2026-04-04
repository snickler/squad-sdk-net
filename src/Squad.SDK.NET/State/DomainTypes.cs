namespace Squad.SDK.NET.State;

public sealed record AgentEntity
{
    public required string Name { get; init; }
    public string? Role { get; init; }
    public string? Description { get; init; }
    public string? Status { get; init; }
    public string? Model { get; init; }
    public IReadOnlyList<string> Expertise { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastActiveAt { get; init; }
}

public sealed record Decision
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? Rationale { get; init; }
    public string? AgentName { get; init; }
    public DecisionStatus Status { get; init; } = DecisionStatus.Proposed;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyList<string> Tags { get; init; } = [];
}

public enum DecisionStatus { Proposed, Accepted, Rejected, Superseded }

public sealed record HistoryEntry
{
    public required string Id { get; init; }
    public required string AgentName { get; init; }
    public required string Action { get; init; }
    public string? Details { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record TeamMember
{
    public required string Name { get; init; }
    public required string Role { get; init; }
    public string? Description { get; init; }
    public string? Model { get; init; }
    public bool IsLead { get; init; }
}

public sealed record Template
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Content { get; init; }
    public string? Category { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}

public sealed record LogEntry
{
    public required string Id { get; init; }
    public required string Level { get; init; }
    public required string Message { get; init; }
    public string? AgentName { get; init; }
    public string? SessionId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
