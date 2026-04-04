using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Agents;
using Squad.SDK.NET.Events;

namespace Squad.SDK.NET.Coordinator;

/// <summary>
/// Provides fan-out orchestration to spawn and dispatch messages to multiple agents in parallel.
/// </summary>
/// <seealso cref="Coordinator"/>
public static class FanOut
{
    /// <summary>
    /// Spawns multiple agents in parallel and sends each the same message.
    /// </summary>
    /// <param name="agentManager">The agent session manager used to spawn and communicate with agents.</param>
    /// <param name="charters">The agent charters defining which agents to spawn.</param>
    /// <param name="message">The message to send to each spawned agent.</param>
    /// <param name="mode">The response tier controlling the depth of agent processing.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A combined list of events collected from all spawned agents.</returns>
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
    /// <param name="agentManager">The agent session manager used to spawn and communicate with agents.</param>
    /// <param name="parentAgentName">The name of the parent agent that owns the sub-agents.</param>
    /// <param name="charters">The agent charters defining which sub-agents to spawn.</param>
    /// <param name="message">The message to send to each spawned sub-agent.</param>
    /// <param name="mode">The response tier controlling the depth of agent processing.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A combined list of events collected from all spawned sub-agents.</returns>
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
