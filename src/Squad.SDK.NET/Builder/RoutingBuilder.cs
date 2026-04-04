using Squad.SDK.NET.Config;
using Squad.SDK.NET.Coordinator;

namespace Squad.SDK.NET.Builder;

public sealed class RoutingBuilder
{
    private readonly List<RoutingRule> _rules = [];
    private string? _defaultAgent;
    private RoutingFallbackBehavior _fallback = RoutingFallbackBehavior.Coordinator;

    public RoutingBuilder AddRule(
        string workType,
        IReadOnlyList<string> agents,
        ResponseTier? tier = null,
        int priority = 0)
    {
        _rules.Add(new RoutingRule
        {
            WorkType = workType,
            Agents = agents,
            Tier = tier,
            Priority = priority
        });
        return this;
    }

    public RoutingBuilder DefaultAgent(string agentName)
    {
        _defaultAgent = agentName;
        return this;
    }

    public RoutingBuilder Fallback(RoutingFallbackBehavior behavior)
    {
        _fallback = behavior;
        return this;
    }

    internal RoutingConfig Build() => new()
    {
        Rules = _rules.AsReadOnly(),
        DefaultAgent = _defaultAgent,
        FallbackBehavior = _fallback
    };
}
