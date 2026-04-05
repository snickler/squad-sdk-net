namespace Squad.SDK.NET.State;

/// <summary>
/// Represents a persisted agent entity in squad state.
/// </summary>
public sealed record AgentEntity
{
    /// <summary>Gets the agent name.</summary>
    public required string Name { get; init; }
    /// <summary>Gets the optional role identifier.</summary>
    public string? Role { get; init; }
    /// <summary>Gets an optional description.</summary>
    public string? Description { get; init; }
    /// <summary>Gets the current agent status.</summary>
    public string? Status { get; init; }
    /// <summary>Gets the optional model identifier.</summary>
    public string? Model { get; init; }
    /// <summary>Gets the areas of expertise.</summary>
    public IReadOnlyList<string> Expertise { get; init; } = [];
    /// <summary>Gets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    /// <summary>Gets the timestamp of last activity, if any.</summary>
    public DateTimeOffset? LastActiveAt { get; init; }
}

/// <summary>
/// Represents an architectural or design decision recorded during squad execution.
/// </summary>
public sealed record Decision
{
    /// <summary>Gets the unique decision identifier.</summary>
    public required string Id { get; init; }
    /// <summary>Gets the decision title.</summary>
    public required string Title { get; init; }
    /// <summary>Gets the decision description.</summary>
    public required string Description { get; init; }
    /// <summary>Gets the optional rationale behind the decision.</summary>
    public string? Rationale { get; init; }
    /// <summary>Gets the name of the agent that made the decision.</summary>
    public string? AgentName { get; init; }
    /// <summary>Gets the current decision status.</summary>
    public DecisionStatus Status { get; init; } = DecisionStatus.Proposed;
    /// <summary>Gets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    /// <summary>Gets the tags associated with this decision.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>Lifecycle status of a <see cref="Decision"/>.</summary>
public enum DecisionStatus
{
    /// <summary>Decision has been proposed but not yet reviewed.</summary>
    Proposed,
    /// <summary>Decision has been accepted.</summary>
    Accepted,
    /// <summary>Decision has been rejected.</summary>
    Rejected,
    /// <summary>Decision has been superseded by another.</summary>
    Superseded
}

/// <summary>
/// Records a single action performed by an agent during squad execution.
/// </summary>
public sealed record HistoryEntry
{
    /// <summary>Gets the unique entry identifier.</summary>
    public required string Id { get; init; }
    /// <summary>Gets the name of the agent that performed the action.</summary>
    public required string AgentName { get; init; }
    /// <summary>Gets the action performed.</summary>
    public required string Action { get; init; }
    /// <summary>Gets optional details about the action.</summary>
    public string? Details { get; init; }
    /// <summary>Gets the action timestamp.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents a member of a squad team.
/// </summary>
public sealed record TeamMember
{
    /// <summary>Gets the member name.</summary>
    public required string Name { get; init; }
    /// <summary>Gets the member's role.</summary>
    public required string Role { get; init; }
    /// <summary>Gets an optional description.</summary>
    public string? Description { get; init; }
    /// <summary>Gets the optional model identifier.</summary>
    public string? Model { get; init; }
    /// <summary>Gets a value indicating whether this member is the team lead.</summary>
    public bool IsLead { get; init; }
}

/// <summary>
/// Represents a reusable template stored in squad state.
/// </summary>
public sealed record Template
{
    /// <summary>Gets the unique template identifier.</summary>
    public required string Id { get; init; }
    /// <summary>Gets the template name.</summary>
    public required string Name { get; init; }
    /// <summary>Gets an optional description.</summary>
    public string? Description { get; init; }
    /// <summary>Gets the template content.</summary>
    public required string Content { get; init; }
    /// <summary>Gets the optional category.</summary>
    public string? Category { get; init; }
    /// <summary>Gets the tags associated with this template.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>
/// Represents a structured log entry from squad execution.
/// </summary>
public sealed record LogEntry
{
    /// <summary>Gets the unique log entry identifier.</summary>
    public required string Id { get; init; }
    /// <summary>Gets the log level (e.g., "info", "warn", "error").</summary>
    public required string Level { get; init; }
    /// <summary>Gets the log message.</summary>
    public required string Message { get; init; }
    /// <summary>Gets the optional agent name that produced this entry.</summary>
    public string? AgentName { get; init; }
    /// <summary>Gets the optional session identifier.</summary>
    public string? SessionId { get; init; }
    /// <summary>Gets the log entry timestamp.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    /// <summary>Gets optional metadata key-value pairs.</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
