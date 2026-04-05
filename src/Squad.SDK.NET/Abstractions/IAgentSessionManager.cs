using Squad.SDK.NET.Agents;
using Squad.SDK.NET.Coordinator;

namespace Squad.SDK.NET.Abstractions;

/// <summary>
/// Manages the lifecycle of agent sessions, including spawning, resuming, and destroying agents.
/// </summary>
public interface IAgentSessionManager
{
    /// <summary>
    /// Spawns a new agent session from the given charter.
    /// </summary>
    /// <param name="charter">The charter defining the agent's identity and capabilities.</param>
    /// <param name="mode">The response tier for the agent session.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="AgentSessionInfo"/> describing the spawned agent.</returns>
    Task<AgentSessionInfo> SpawnAsync(AgentCharter charter, ResponseTier mode = ResponseTier.Standard, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a previously spawned agent session by name.
    /// </summary>
    /// <param name="agentName">The name of the agent to resume.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="AgentSessionInfo"/> for the resumed agent.</returns>
    Task<AgentSessionInfo> ResumeAsync(string agentName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the session info for a specific agent by name, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="name">The name of the agent.</param>
    /// <returns>The <see cref="AgentSessionInfo"/> if found; otherwise, <see langword="null"/>.</returns>
    AgentSessionInfo? GetAgent(string name);

    /// <summary>
    /// Gets session info for all currently active agents.
    /// </summary>
    /// <returns>A read-only list of <see cref="AgentSessionInfo"/> entries.</returns>
    IReadOnlyList<AgentSessionInfo> GetAllAgents();

    /// <summary>
    /// Destroys an agent session and releases its resources.
    /// </summary>
    /// <param name="agentName">The name of the agent to destroy.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the agent has been destroyed.</returns>
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
