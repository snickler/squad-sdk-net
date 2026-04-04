using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Builder;

public sealed class BudgetBuilder
{
    private decimal? _perAgentSpawn;
    private decimal? _perSession;
    private decimal? _warnAt;

    public BudgetBuilder PerAgentSpawn(decimal amount) { _perAgentSpawn = amount; return this; }
    public BudgetBuilder PerSession(decimal amount) { _perSession = amount; return this; }
    public BudgetBuilder WarnAt(decimal threshold) { _warnAt = threshold; return this; }

    internal BudgetConfig Build() => new()
    {
        PerAgentSpawn = _perAgentSpawn,
        PerSession = _perSession,
        WarnAt = _warnAt
    };
}
