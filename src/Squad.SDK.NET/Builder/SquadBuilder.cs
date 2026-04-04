using Squad.SDK.NET.Config;
using Squad.SDK.NET.Hooks;

namespace Squad.SDK.NET.Builder;

/// <summary>
/// Fluent entry point for building a <see cref="SquadConfig"/> — equivalent to <c>defineSquad()</c>.
/// </summary>
public sealed class SquadBuilder
{
    private TeamConfig? _team;
    private readonly List<AgentConfig> _agents = [];
    private RoutingConfig? _routing;
    private ModelSelectionConfig? _models;
    private PolicyConfig? _hooks;
    private DefaultsConfig? _defaults;
    private readonly List<CeremonyConfig> _ceremonies = [];
    private CastingConfig? _casting;
    private TelemetryConfig? _telemetry;
    private readonly List<SkillConfig> _skills = [];
    private BudgetConfig? _budget;
    private HooksDefinition? _hooksDefinition;

    public SquadBuilder WithTeam(Action<TeamBuilder> configure)
    {
        var builder = new TeamBuilder();
        configure(builder);
        _team = builder.Build();
        return this;
    }

    public SquadBuilder WithAgent(Action<AgentBuilder> configure)
    {
        var builder = new AgentBuilder();
        configure(builder);
        _agents.Add(builder.Build());
        return this;
    }

    public SquadBuilder WithRouting(Action<RoutingBuilder> configure)
    {
        var builder = new RoutingBuilder();
        configure(builder);
        _routing = builder.Build();
        return this;
    }

    public SquadBuilder WithModels(Action<ModelBuilder> configure)
    {
        var builder = new ModelBuilder();
        configure(builder);
        _models = builder.Build();
        return this;
    }

    public SquadBuilder WithHooks(PolicyConfig policy)
    {
        _hooks = policy;
        _hooksDefinition = new HooksDefinition
        {
            AllowedWritePaths = policy.AllowedWritePaths,
            BlockedCommands = policy.BlockedCommands,
            MaxAskUserPerSession = policy.MaxAskUserPerSession,
            ScrubPii = policy.ScrubPii,
            ReviewerLockout = policy.ReviewerLockout
        };
        return this;
    }

    public SquadBuilder WithDefaults(Action<DefaultsBuilder> configure)
    {
        var builder = new DefaultsBuilder();
        configure(builder);
        _defaults = builder.Build();
        return this;
    }

    public SquadBuilder WithCeremony(Action<CeremonyBuilder> configure)
    {
        var builder = new CeremonyBuilder();
        configure(builder);
        _ceremonies.Add(builder.Build());
        return this;
    }

    public SquadBuilder WithCasting(Action<CastingBuilder> configure)
    {
        var builder = new CastingBuilder();
        configure(builder);
        _casting = builder.Build();
        return this;
    }

    public SquadBuilder WithTelemetry(Action<TelemetryBuilder> configure)
    {
        var builder = new TelemetryBuilder();
        configure(builder);
        _telemetry = builder.Build();
        return this;
    }

    public SquadBuilder WithSkill(Action<SkillBuilder> configure)
    {
        var builder = new SkillBuilder();
        configure(builder);
        _skills.Add(builder.Build());
        return this;
    }

    public SquadBuilder WithBudget(Action<BudgetBuilder> configure)
    {
        var builder = new BudgetBuilder();
        configure(builder);
        _budget = builder.Build();
        return this;
    }

    public SquadBuilder WithHooks(Action<HooksBuilder> configure)
    {
        var builder = new HooksBuilder();
        configure(builder);
        _hooksDefinition = builder.Build();
        return this;
    }

    /// <summary>Validates and returns the built <see cref="SquadConfig"/>.</summary>
    public SquadConfig Build()
    {
        if (_team is null)
            throw new InvalidOperationException("A team configuration is required. Call WithTeam() before building.");

        return new SquadConfig
        {
            Team = _team,
            Agents = _agents.AsReadOnly(),
            Routing = _routing,
            Models = _models,
            Defaults = _defaults,
            Ceremonies = _ceremonies.Count > 0 ? _ceremonies.AsReadOnly() : null,
            Casting = _casting,
            Telemetry = _telemetry,
            Skills = _skills.Count > 0 ? _skills.AsReadOnly() : null,
            Hooks = _hooksDefinition,
            Budget = _budget
        };
    }

    /// <summary>Static entry point — equivalent to <c>defineSquad()</c>.</summary>
    public static SquadBuilder Create() => new();
}
