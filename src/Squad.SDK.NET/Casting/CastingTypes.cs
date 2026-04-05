namespace Squad.SDK.NET.Casting;

/// <summary>
/// Represents a cast agent with a persona, universe, and traits.
/// </summary>
public sealed record CastMember
{
    /// <summary>Gets the cast member's display name.</summary>
    public required string Name { get; init; }
    /// <summary>Gets the persona description.</summary>
    public required string Persona { get; init; }
    /// <summary>Gets the universe this member was cast from.</summary>
    public required string Universe { get; init; }
    /// <summary>Gets an optional catchphrase for the persona.</summary>
    public string? Catchphrase { get; init; }
    /// <summary>Gets the traits assigned to this cast member.</summary>
    public IReadOnlyList<string> Traits { get; init; } = [];
    /// <summary>Gets the timestamp when this member was cast.</summary>
    public DateTimeOffset CastAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Associates a cast member with the agent name and role that produced it.
/// </summary>
public sealed record CastingRecord
{
    /// <summary>Gets the original agent name.</summary>
    public required string AgentName { get; init; }
    /// <summary>Gets the resulting <see cref="CastMember"/>.</summary>
    public required CastMember Member { get; init; }
    /// <summary>Gets the role identifier used during casting.</summary>
    public required string RoleId { get; init; }
    /// <summary>Gets the timestamp when the agent was assigned.</summary>
    public DateTimeOffset AssignedAt { get; init; } = DateTimeOffset.UtcNow;
}
