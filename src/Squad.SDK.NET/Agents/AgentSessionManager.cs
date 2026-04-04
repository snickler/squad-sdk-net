using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Coordinator;
using Squad.SDK.NET.Events;

namespace Squad.SDK.NET.Agents;

public sealed class AgentSessionManager : IAgentSessionManager
{
    private readonly ISquadClient _client;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AgentSessionManager> _logger;
    private readonly ConcurrentDictionary<string, AgentSessionInfo> _agents = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ISquadSession> _sessions = new(StringComparer.OrdinalIgnoreCase);

    public AgentSessionManager(ISquadClient client, IEventBus eventBus, ILogger<AgentSessionManager> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentSessionInfo> SpawnAsync(AgentCharter charter, ResponseTier mode = ResponseTier.Standard, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Spawning agent '{AgentName}' with model '{Model}'", charter.Name, charter.ModelPreference);
        
        var info = new AgentSessionInfo
        {
            Charter = charter,
            State = AgentState.Spawning,
            ResponseMode = mode,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _agents[charter.Name] = info;

        await _eventBus.EmitAsync(new SquadEvent
        {
            Type = SquadEventType.AgentMilestone,
            AgentName = charter.Name,
            Payload = AgentState.Spawning,
        }, cancellationToken);

        try
        {
            var config = new SquadSessionConfig
            {
                ClientName = charter.Name,
                Model = charter.ModelPreference,
                SystemMessage = charter.Prompt,
                AvailableTools = charter.AllowedTools,
                ExcludedTools = charter.ExcludedTools,
            };

            var session = await _client.CreateSessionAsync(config, cancellationToken);

            info.SessionId = session.SessionId;
            info.State = AgentState.Active;
            info.LastActiveAt = DateTimeOffset.UtcNow;
            _sessions[charter.Name] = session;

            _logger.LogInformation("Agent '{AgentName}' active, session {SessionId}", charter.Name, session.SessionId);

            await _eventBus.EmitAsync(new SquadEvent
            {
                Type = SquadEventType.AgentMilestone,
                AgentName = charter.Name,
                SessionId = session.SessionId,
                Payload = AgentState.Active,
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to spawn agent '{AgentName}'", charter.Name);
            info.State = AgentState.Error;

            await _eventBus.EmitAsync(new SquadEvent
            {
                Type = SquadEventType.SessionError,
                AgentName = charter.Name,
                Payload = AgentState.Error,
            }, cancellationToken);

            throw;
        }

        return info;
    }

    public async Task<AgentSessionInfo> ResumeAsync(string agentName, CancellationToken cancellationToken = default)
    {
        if (!_agents.TryGetValue(agentName, out var info))
            throw new InvalidOperationException($"No agent found with name '{agentName}'.");

        if (info.SessionId is null)
            throw new InvalidOperationException($"Agent '{agentName}' has no session ID to resume.");

        var session = await _client.ResumeSessionAsync(info.SessionId, cancellationToken);
        _sessions[agentName] = session;

        info.State = AgentState.Active;
        info.LastActiveAt = DateTimeOffset.UtcNow;

        await _eventBus.EmitAsync(new SquadEvent
        {
            Type = SquadEventType.AgentMilestone,
            AgentName = agentName,
            SessionId = info.SessionId,
            Payload = AgentState.Active,
        }, cancellationToken);

        return info;
    }

    public AgentSessionInfo? GetAgent(string name) =>
        _agents.TryGetValue(name, out var info) ? info : null;

    public IReadOnlyList<AgentSessionInfo> GetAllAgents() =>
        [.. _agents.Values];

    public ISquadSession? GetSession(string agentName) =>
        _sessions.TryGetValue(agentName, out var session) ? session : null;

    public async Task DestroyAsync(string agentName, CancellationToken cancellationToken = default)
    {
        if (!_agents.TryGetValue(agentName, out var info)) return;

        _logger.LogInformation("Destroying agent '{AgentName}'", agentName);

        // CASCADE: Destroy sub-agents first (depth-first)
        foreach (var subAgentName in info.SubAgentNames.ToList())
        {
            await DestroyAsync(subAgentName, cancellationToken);
        }

        // Remove from parent's sub-agent list
        if (info.ParentAgentName is not null && _agents.TryGetValue(info.ParentAgentName, out var parent))
        {
            parent.SubAgentNames.Remove(agentName);
        }

        if (_sessions.TryRemove(agentName, out var session))
        {
            await session.DisposeAsync();
        }

        info.State = AgentState.Destroyed;
        _agents.TryRemove(agentName, out _);

        await _eventBus.EmitAsync(new SquadEvent
        {
            Type = SquadEventType.SessionDestroyed,
            AgentName = agentName,
            SessionId = info.SessionId,
            Payload = AgentState.Destroyed,
        }, cancellationToken);
    }

    public async Task<AgentSessionInfo> SpawnSubAgentAsync(
        string parentAgentName,
        AgentCharter charter,
        ResponseTier mode = ResponseTier.Standard,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate parent exists and is Active
        var parent = GetAgent(parentAgentName)
            ?? throw new InvalidOperationException($"Parent agent '{parentAgentName}' not found.");
        
        if (parent.State != AgentState.Active)
            throw new InvalidOperationException($"Parent agent '{parentAgentName}' is not active (state: {parent.State}).");
        
        // 2. Enforce max depth (3 levels to prevent runaway nesting)
        const int MaxDepth = 3;
        if (parent.Depth >= MaxDepth)
            throw new InvalidOperationException($"Maximum sub-agent depth ({MaxDepth}) exceeded.");
        
        // 3. Spawn using existing SpawnAsync
        var info = await SpawnAsync(charter, mode, cancellationToken);
        
        // 4. Set parent-child relationship
        info.ParentAgentName = parentAgentName;
        info.Depth = parent.Depth + 1;
        parent.SubAgentNames.Add(charter.Name);
        
        // 5. Emit event for sub-agent spawn
        await _eventBus.EmitAsync(new SquadEvent
        {
            Type = SquadEventType.AgentMilestone,
            AgentName = charter.Name,
            SessionId = info.SessionId,
            Payload = new SubAgentSpawnedPayload
            {
                ParentAgentName = parentAgentName,
                ChildAgentName = charter.Name,
                Depth = info.Depth
            }
        }, cancellationToken);
        
        return info;
    }

    public IReadOnlyList<AgentSessionInfo> GetSubAgents(string parentAgentName)
    {
        return _agents.Values
            .Where(a => a.ParentAgentName == parentAgentName)
            .ToList();
    }

    public IReadOnlyList<AgentSessionInfo> GetAgentTree(string rootAgentName)
    {
        var tree = new List<AgentSessionInfo>();
        CollectTree(rootAgentName, tree);
        return tree;
    }

    private void CollectTree(string agentName, List<AgentSessionInfo> results)
    {
        if (!_agents.TryGetValue(agentName, out var info)) return;
        results.Add(info);
        foreach (var child in info.SubAgentNames)
        {
            CollectTree(child, results);
        }
    }
}
