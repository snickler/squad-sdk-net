namespace Squad.SDK.NET.Casting;

public sealed record CastMember
{
    public required string Name { get; init; }
    public required string Persona { get; init; }
    public required string Universe { get; init; }
    public string? Catchphrase { get; init; }
    public IReadOnlyList<string> Traits { get; init; } = [];
    public DateTimeOffset CastAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record CastingRecord
{
    public required string AgentName { get; init; }
    public required CastMember Member { get; init; }
    public required string RoleId { get; init; }
    public DateTimeOffset AssignedAt { get; init; } = DateTimeOffset.UtcNow;
}
