namespace Squad.SDK.NET.Agents;

/// <summary>Represents the lifecycle state of an agent.</summary>
public enum AgentState
{
    /// <summary>The agent has been created but not yet started.</summary>
    Pending,

    /// <summary>The agent session is currently being initialized.</summary>
    Spawning,

    /// <summary>The agent is active and processing work.</summary>
    Active,

    /// <summary>The agent is idle and waiting for new work.</summary>
    Idle,

    /// <summary>The agent has encountered an error.</summary>
    Error,

    /// <summary>The agent has been destroyed and its session released.</summary>
    Destroyed
}
