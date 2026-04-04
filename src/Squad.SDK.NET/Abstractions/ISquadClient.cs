using Squad.SDK.NET.Events;

namespace Squad.SDK.NET.Abstractions;

/// <summary>
/// High-level client for managing a squad of AI agents.
/// Wraps CopilotClient with multi-session orchestration.
/// </summary>
public interface ISquadClient : IAsyncDisposable
{
    /// <summary>
    /// Connects to the Copilot platform and starts the client.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the client is connected and ready.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gracefully disconnects and stops the client.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the client has stopped.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new agent session with the specified configuration.
    /// </summary>
    /// <param name="config">Optional session configuration; uses defaults when <see langword="null"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created <see cref="ISquadSession"/>.</returns>
    /// <seealso cref="ResumeSessionAsync"/>
    Task<ISquadSession> CreateSessionAsync(SquadSessionConfig? config = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes an existing session by its identifier.
    /// </summary>
    /// <param name="sessionId">The identifier of the session to resume.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The resumed <see cref="ISquadSession"/>.</returns>
    /// <seealso cref="CreateSessionAsync"/>
    Task<ISquadSession> ResumeSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a session and its history.
    /// </summary>
    /// <param name="sessionId">The identifier of the session to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the session has been deleted.</returns>
    Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists metadata for all sessions owned by this client.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of <see cref="SquadSessionMetadata"/> entries.</returns>
    Task<IReadOnlyList<SquadSessionMetadata>> ListSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists available AI models with their capabilities.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of <see cref="SquadModelInfo"/> entries.</returns>
    Task<IReadOnlyList<SquadModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current connection state of the client.
    /// </summary>
    /// <seealso cref="ConnectionState"/>
    ConnectionState State { get; }

    /// <summary>
    /// Subscribes to all events emitted by this client.
    /// </summary>
    /// <param name="handler">A callback invoked for each <see cref="SquadEvent"/>.</param>
    /// <returns>An <see cref="IDisposable"/> that removes the subscription when disposed.</returns>
    IDisposable On(Action<SquadEvent> handler);
}
