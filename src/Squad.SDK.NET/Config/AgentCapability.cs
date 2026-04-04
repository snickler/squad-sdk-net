namespace Squad.SDK.NET.Config;

public sealed record AgentCapability
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool Enabled { get; init; } = true;
}
