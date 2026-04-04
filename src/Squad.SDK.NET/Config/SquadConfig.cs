using Squad.SDK.NET.Hooks;

namespace Squad.SDK.NET.Config;

public sealed record SquadConfig
{
    public string Version { get; init; } = "1.0";
    public required TeamConfig Team { get; init; }
    public RoutingConfig? Routing { get; init; }
    public ModelSelectionConfig? Models { get; init; }
    public IReadOnlyList<AgentConfig> Agents { get; init; } = [];
    public DefaultsConfig? Defaults { get; init; }
    public IReadOnlyList<CeremonyConfig>? Ceremonies { get; init; }
    public CastingConfig? Casting { get; init; }
    public TelemetryConfig? Telemetry { get; init; }
    public IReadOnlyList<SkillConfig>? Skills { get; init; }
    public HooksDefinition? Hooks { get; init; }
    public BudgetConfig? Budget { get; init; }
}
