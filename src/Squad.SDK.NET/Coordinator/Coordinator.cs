using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Agents;
using Squad.SDK.NET.Config;
using Squad.SDK.NET.Events;

namespace Squad.SDK.NET.Coordinator;

public sealed class Coordinator : ICoordinator
{
    private readonly SquadConfig _config;
    private readonly IAgentSessionManager _agentManager;
    private readonly IEventBus _eventBus;
    private readonly ILogger<Coordinator> _logger;
    private IReadOnlyList<RoutingRule> _rules = [];

    public Coordinator(SquadConfig config, IAgentSessionManager agentManager, IEventBus eventBus, ILogger<Coordinator> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _agentManager = agentManager ?? throw new ArgumentNullException(nameof(agentManager));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _rules = _config.Routing?.Rules ?? [];
        return Task.CompletedTask;
    }

    public async Task<RoutingDecision> RouteAsync(string message, CancellationToken cancellationToken = default)
    {
        var normalized = message.ToLowerInvariant();

        // Match routing rules by work type keywords
        var matched = _rules
            .Where(r => ContainsWorkTypeKeyword(normalized, r.WorkType))
            .OrderByDescending(r => r.Priority)
            .FirstOrDefault();

        RoutingDecision decision;

        if (matched is not null)
        {
            decision = new RoutingDecision
            {
                Tier = matched.Tier ?? SelectResponseTier(normalized),
                Agents = matched.Agents,
                Parallel = matched.Agents.Count > 1,
                Rationale = $"Matched routing rule for work type '{matched.WorkType}'",
            };
        }
        else
        {
            decision = ApplyFallback(normalized);
        }

        _logger.LogDebug("Routing to {Agents} (parallel={Parallel}): {Rationale}", 
            string.Join(", ", decision.Agents), decision.Parallel, decision.Rationale);

        await _eventBus.EmitAsync(new SquadEvent
        {
            Type = SquadEventType.CoordinatorRouting,
            Payload = decision,
        }, cancellationToken);

        return decision;
    }

    public async Task ExecuteAsync(RoutingDecision decision, string message, CancellationToken cancellationToken = default)
    {
        var options = new SquadMessageOptions { Prompt = message };

        if (decision.Parallel)
        {
            var sendTasks = decision.Agents.Select(async agentName =>
            {
                var info = _agentManager.GetAgent(agentName);
                if (info is null || info.SessionId is null) return;

                if (_agentManager is AgentSessionManager manager)
                {
                    var session = manager.GetSession(agentName);
                    if (session is not null)
                    {
                        await session.SendAsync(options, cancellationToken);
                    }
                }
            });

            await Task.WhenAll(sendTasks);
        }
        else
        {
            foreach (var agentName in decision.Agents)
            {
                if (_agentManager is AgentSessionManager manager)
                {
                    var session = manager.GetSession(agentName);
                    if (session is not null)
                    {
                        await session.SendAsync(options, cancellationToken);
                    }
                }
            }
        }
    }

    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _rules = [];
        return Task.CompletedTask;
    }

    private static bool ContainsWorkTypeKeyword(string message, string workType)
    {
        // Split work type by dash and check if the message contains any of the keywords
        var keywords = workType.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return keywords.Any(kw => message.Contains(kw, StringComparison.OrdinalIgnoreCase));
    }

    private static ResponseTier SelectResponseTier(string message)
    {
        // Heuristic: longer, more complex messages → higher tier
        if (message.Length < 50 && !message.Contains("architect") && !message.Contains("design"))
            return ResponseTier.Lightweight;

        if (message.Contains("architect") || message.Contains("design") || message.Contains("review") || message.Length > 300)
            return ResponseTier.Full;

        return ResponseTier.Standard;
    }

    private RoutingDecision ApplyFallback(string message)
    {
        var fallback = _config.Routing?.FallbackBehavior ?? RoutingFallbackBehavior.Coordinator;

        return fallback switch
        {
            RoutingFallbackBehavior.DefaultAgent when _config.Routing?.DefaultAgent is { } defaultAgent =>
                new RoutingDecision
                {
                    Tier = SelectResponseTier(message),
                    Agents = [defaultAgent],
                    Parallel = false,
                    Rationale = "No rule matched; using default agent",
                },

            RoutingFallbackBehavior.Ask =>
                new RoutingDecision
                {
                    Tier = ResponseTier.Direct,
                    Agents = [],
                    Parallel = false,
                    Rationale = "No rule matched; requires clarification",
                },

            _ =>
                new RoutingDecision
                {
                    Tier = SelectResponseTier(message),
                    Agents = _agentManager.GetAllAgents()
                        .Where(a => a.State == AgentState.Active)
                        .Select(a => a.Charter.Name)
                        .ToList(),
                    Parallel = true,
                    Rationale = "No rule matched; routing to coordinator (all active agents)",
                },
        };
    }
}
