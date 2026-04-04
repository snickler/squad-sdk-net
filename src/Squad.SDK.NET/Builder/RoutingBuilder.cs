using Squad.SDK.NET.Config;
using Squad.SDK.NET.Coordinator;

namespace Squad.SDK.NET.Builder;

/// <summary>
/// Fluent builder for configuring a <see cref="RoutingConfig"/>.
/// </summary>
/// <seealso cref="SquadBuilder"/>
public sealed class RoutingBuilder
{
    private readonly List<RoutingRule> _rules = [];
    private string? _defaultAgent;
    private RoutingFallbackBehavior _fallback = RoutingFallbackBehavior.Coordinator;

    /// <summary>Adds a routing rule that maps a work type to one or more agents.</summary>
    /// <param name="workType">The type of work to match (e.g., <c>"code"</c>, <c>"review"</c>).</param>
    /// <param name="agents">Agent names eligible for this work type.</param>
    /// <param name="tier">Optional response tier override for this rule.</param>
    /// <param name="priority">Priority of the rule; higher values are evaluated first.</param>
    /// <returns>This builder instance for chaining.</returns>
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

    /// <summary>Sets the default agent used when no routing rule matches.</summary>
    /// <param name="agentName">The name of the fallback agent.</param>
    /// <returns>This builder instance for chaining.</returns>
    public RoutingBuilder DefaultAgent(string agentName)
    {
        _defaultAgent = agentName;
        return this;
    }

    /// <summary>Sets the fallback behavior when no rule or default agent applies.</summary>
    /// <param name="behavior">The <see cref="RoutingFallbackBehavior"/> to use.</param>
    /// <returns>This builder instance for chaining.</returns>
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
