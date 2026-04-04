using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Agents;
using Squad.SDK.NET.Events;

namespace Squad.SDK.NET.Coordinator;

public static class FanOut
{
    public static async Task<IReadOnlyList<SquadEvent>> SpawnParallelAsync(
        IAgentSessionManager agentManager,
        IReadOnlyList<AgentCharter> charters,
        string message,
        ResponseTier mode = ResponseTier.Standard,
        CancellationToken cancellationToken = default)
    {
        // Spawn all agents in parallel
        var spawnTasks = charters
            .Select(charter => agentManager.SpawnAsync(charter, mode, cancellationToken))
            .ToList();

        var sessionInfos = await Task.WhenAll(spawnTasks);

        // Send message to each active agent and collect events
        var sendTasks = sessionInfos
            .Where(info => info.State == AgentState.Active)
            .Select(async info =>
            {
                var session = GetSession(agentManager, info.Charter.Name);
                if (session is null) return (IReadOnlyList<SquadEvent>)[];

                var options = new SquadMessageOptions { Prompt = message };
                await session.SendAsync(options, cancellationToken);
                return await session.GetMessagesAsync(cancellationToken);
            })
            .ToList();

        var results = await Task.WhenAll(sendTasks);

        return [.. results.SelectMany(r => r)];
    }

    private static ISquadSession? GetSession(IAgentSessionManager agentManager, string agentName)
    {
        if (agentManager is AgentSessionManager manager)
        {
            return manager.GetSession(agentName);
        }

        return null;
    }

    /// <summary>
    /// Spawns multiple sub-agents under a parent agent and sends them all the same message.
    /// </summary>
    public static async Task<IReadOnlyList<SquadEvent>> SpawnSubAgentsParallelAsync(
        IAgentSessionManager agentManager,
        string parentAgentName,
        IReadOnlyList<AgentCharter> charters,
        string message,
        ResponseTier mode = ResponseTier.Standard,
        CancellationToken cancellationToken = default)
    {
        var spawnTasks = charters
            .Select(charter => agentManager.SpawnSubAgentAsync(parentAgentName, charter, mode, cancellationToken))
            .ToList();

        var sessionInfos = await Task.WhenAll(spawnTasks);

        var sendTasks = sessionInfos
            .Where(info => info.State == AgentState.Active)
            .Select(async info =>
            {
                var session = GetSession(agentManager, info.Charter.Name);
                if (session is null) return (IReadOnlyList<SquadEvent>)[];

                var options = new SquadMessageOptions { Prompt = message };
                await session.SendAsync(options, cancellationToken);
                return await session.GetMessagesAsync(cancellationToken);
            })
            .ToList();

        var results = await Task.WhenAll(sendTasks);
        return [.. results.SelectMany(r => r)];
    }
}
