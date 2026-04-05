namespace Squad.SDK.NET.Config;

/// <summary>Represents the administrative status of an agent within the squad configuration.</summary>
public enum AgentStatus
{
    /// <summary>The agent is active and available for work assignment.</summary>
    Active,

    /// <summary>The agent is temporarily inactive and will not receive new work.</summary>
    Inactive,

    /// <summary>The agent has been retired and is no longer part of the squad.</summary>
    Retired
}
