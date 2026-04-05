namespace Squad.SDK.NET.Config;

/// <summary>Configuration record that defines a single agent within a squad.</summary>
/// <seealso cref="SquadConfig" />
public sealed record AgentConfig
{
    /// <summary>Gets the unique name of the agent.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the display-friendly name of the agent.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Gets the role the agent fulfils within the squad.</summary>
    public required string Role { get; init; }

    /// <summary>Gets the list of expertise areas for the agent.</summary>
    public IReadOnlyList<string> Expertise { get; init; } = [];

    /// <summary>Gets the communication style hint for the agent.</summary>
    public string? Style { get; init; }

    /// <summary>Gets the system prompt used to initialize the agent.</summary>
    public string? Prompt { get; init; }

    /// <summary>Gets the preferred model identifier as a simple string.</summary>
    public string? ModelPreference { get; init; }

    /// <summary>Gets the detailed model preference with fallback information.</summary>
    public ModelPreference? ModelPreferenceDetail { get; init; }

    /// <summary>Gets the explicit list of tools the agent is allowed to use.</summary>
    public IReadOnlyList<string>? AllowedTools { get; init; }

    /// <summary>Gets the list of tools the agent is forbidden from using.</summary>
    public IReadOnlyList<string>? ExcludedTools { get; init; }

    /// <summary>Gets the list of capabilities the agent supports.</summary>
    public IReadOnlyList<AgentCapability>? Capabilities { get; init; }

    /// <summary>Gets the optional per-agent budget constraints.</summary>
    public BudgetConfig? Budget { get; init; }

    /// <summary>Gets the current status of the agent.</summary>
    public AgentStatus Status { get; init; } = AgentStatus.Active;

    /// <summary>Gets the file path to the agent's charter definition.</summary>
    public string? Charter { get; init; }
}
