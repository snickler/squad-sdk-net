using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Builder;

/// <summary>
/// Fluent builder for configuring a <see cref="BudgetConfig"/>.
/// </summary>
/// <seealso cref="SquadBuilder"/>
public sealed class BudgetBuilder
{
    private decimal? _perAgentSpawn;
    private decimal? _perSession;
    private decimal? _warnAt;

    /// <summary>Sets the maximum cost allowed per agent spawn.</summary>
    /// <param name="amount">The cost limit in the configured currency.</param>
    /// <returns>This builder instance for chaining.</returns>
    public BudgetBuilder PerAgentSpawn(decimal amount) { _perAgentSpawn = amount; return this; }

    /// <summary>Sets the maximum cost allowed per session.</summary>
    /// <param name="amount">The cost limit in the configured currency.</param>
    /// <returns>This builder instance for chaining.</returns>
    public BudgetBuilder PerSession(decimal amount) { _perSession = amount; return this; }

    /// <summary>Sets the cost threshold at which a warning is emitted.</summary>
    /// <param name="threshold">The warning threshold amount.</param>
    /// <returns>This builder instance for chaining.</returns>
    public BudgetBuilder WarnAt(decimal threshold) { _warnAt = threshold; return this; }

    internal BudgetConfig Build() => new()
    {
        PerAgentSpawn = _perAgentSpawn,
        PerSession = _perSession,
        WarnAt = _warnAt
    };
}
