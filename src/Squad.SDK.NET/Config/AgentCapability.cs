namespace Squad.SDK.NET.Config;

/// <summary>Represents a capability that an agent can possess, such as code generation or testing.</summary>
public sealed record AgentCapability
{
    /// <summary>Gets the name of the capability.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the human-readable description of the capability.</summary>
    public string? Description { get; init; }

    /// <summary>Gets a value indicating whether this capability is enabled.</summary>
    public bool Enabled { get; init; } = true;
}
