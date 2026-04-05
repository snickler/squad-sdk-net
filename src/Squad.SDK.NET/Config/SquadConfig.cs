using Squad.SDK.NET.Hooks;

namespace Squad.SDK.NET.Config;

/// <summary>Root configuration record for a Squad, encompassing team, agents, routing, and operational settings.</summary>
/// <seealso cref="ConfigLoader"/>
public sealed record SquadConfig
{
    /// <summary>Gets the configuration schema version.</summary>
    public string Version { get; init; } = "1.0";

    /// <summary>Gets the team-level configuration.</summary>
    public required TeamConfig Team { get; init; }

    /// <summary>Gets the optional routing configuration that maps work types to agents.</summary>
    public RoutingConfig? Routing { get; init; }

    /// <summary>Gets the optional model selection configuration.</summary>
    public ModelSelectionConfig? Models { get; init; }

    /// <summary>Gets the list of agent configurations.</summary>
    public IReadOnlyList<AgentConfig> Agents { get; init; } = [];

    /// <summary>Gets the optional default settings applied to all agents.</summary>
    public DefaultsConfig? Defaults { get; init; }

    /// <summary>Gets the optional list of ceremony configurations.</summary>
    public IReadOnlyList<CeremonyConfig>? Ceremonies { get; init; }

    /// <summary>Gets the optional casting configuration for agent pool management.</summary>
    public CastingConfig? Casting { get; init; }

    /// <summary>Gets the optional telemetry configuration.</summary>
    public TelemetryConfig? Telemetry { get; init; }

    /// <summary>Gets the optional list of skill configurations available to agents.</summary>
    public IReadOnlyList<SkillConfig>? Skills { get; init; }

    /// <summary>Gets the optional hooks definition for policy enforcement.</summary>
    public HooksDefinition? Hooks { get; init; }

    /// <summary>Gets the optional budget constraints for the squad.</summary>
    public BudgetConfig? Budget { get; init; }
}
