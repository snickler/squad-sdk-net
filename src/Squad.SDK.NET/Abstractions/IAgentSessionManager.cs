using Squad.SDK.NET.Agents;
using Squad.SDK.NET.Coordinator;

namespace Squad.SDK.NET.Abstractions;

public interface IAgentSessionManager
{
    Task<AgentSessionInfo> SpawnAsync(AgentCharter charter, ResponseTier mode = ResponseTier.Standard, CancellationToken cancellationToken = default);
    Task<AgentSessionInfo> ResumeAsync(string agentName, CancellationToken cancellationToken = default);
    AgentSessionInfo? GetAgent(string name);
    IReadOnlyList<AgentSessionInfo> GetAllAgents();
    Task DestroyAsync(string agentName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Spawns a sub-agent under a parent agent. The sub-agent's lifecycle is tied to the parent.
    /// </summary>
    Task<AgentSessionInfo> SpawnSubAgentAsync(
        string parentAgentName,
        AgentCharter charter,
        ResponseTier mode = ResponseTier.Standard,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all direct sub-agents of the specified parent agent.
    /// </summary>
    IReadOnlyList<AgentSessionInfo> GetSubAgents(string parentAgentName);

    /// <summary>
    /// Gets the full agent hierarchy tree rooted at the specified agent.
    /// </summary>
    IReadOnlyList<AgentSessionInfo> GetAgentTree(string rootAgentName);
}
