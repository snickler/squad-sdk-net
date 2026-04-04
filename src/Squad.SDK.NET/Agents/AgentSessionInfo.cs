using Squad.SDK.NET.Coordinator;

namespace Squad.SDK.NET.Agents;

/// <summary>Holds runtime information about an active or previously active agent session.</summary>
/// <seealso cref="AgentCharter"/>
/// <seealso cref="AgentSessionManager"/>
public sealed record AgentSessionInfo
{
    /// <summary>Gets the charter that defines this agent's identity and configuration.</summary>
    public required AgentCharter Charter { get; init; }

    /// <summary>Gets or sets the unique session identifier assigned by the backend.</summary>
    public string? SessionId { get; set; }

    /// <summary>Gets or sets the current lifecycle state of the agent.</summary>
    public AgentState State { get; set; } = AgentState.Pending;

    /// <summary>Gets or sets the timestamp when the agent session was created.</summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>Gets or sets the timestamp of the agent's last recorded activity.</summary>
    public DateTimeOffset? LastActiveAt { get; set; }

    /// <summary>Gets or sets the response tier used for this agent's session.</summary>
    public ResponseTier ResponseMode { get; set; } = ResponseTier.Standard;

    /// <summary>Gets or sets the name of the parent agent, or <see langword="null"/> if this is a root agent.</summary>
    public string? ParentAgentName { get; set; }

    /// <summary>Gets or sets the names of sub-agents spawned by this agent.</summary>
    public List<string> SubAgentNames { get; set; } = [];

    /// <summary>Gets or sets the nesting depth of this agent in the agent hierarchy.</summary>
    public int Depth { get; set; }
}
