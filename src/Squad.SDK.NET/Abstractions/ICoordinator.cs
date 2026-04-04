using Squad.SDK.NET.Coordinator;

namespace Squad.SDK.NET.Abstractions;

/// <summary>
/// Coordinates message routing among agents in a squad.
/// </summary>
public interface ICoordinator
{
    /// <summary>
    /// Initializes the coordinator and any required resources.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when initialization is finished.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines which agent should handle the given message.
    /// </summary>
    /// <param name="message">The user message to route.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="RoutingDecision"/> indicating the target agent and response tier.</returns>
    Task<RoutingDecision> RouteAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a routing decision by dispatching the message to the selected agent.
    /// </summary>
    /// <param name="decision">The routing decision returned by <see cref="RouteAsync"/>.</param>
    /// <param name="message">The user message to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when execution is finished.</returns>
    Task ExecuteAsync(RoutingDecision decision, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuts down the coordinator and releases associated resources.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when shutdown is finished.</returns>
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}
