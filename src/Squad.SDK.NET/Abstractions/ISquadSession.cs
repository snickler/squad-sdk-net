using Squad.SDK.NET.Events;

namespace Squad.SDK.NET.Abstractions;

/// <summary>
/// Represents an active agent session for sending messages and receiving responses.
/// </summary>
/// <seealso cref="ISquadClient"/>
public interface ISquadSession : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique identifier for this session.
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Gets the workspace directory path associated with this session, if any.
    /// </summary>
    string? WorkspacePath { get; }

    /// <summary>
    /// Sends a message and returns the turn ID (not the response content).
    /// Use <see cref="SendAndWaitAsync"/> to get the actual response text.
    /// </summary>
    Task<string> SendAsync(SquadMessageOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message and waits for the complete assistant response.
    /// Returns the response content text, or null if the model produced no response.
    /// </summary>
    Task<string?> SendAndWaitAsync(SquadMessageOptions options, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aborts the currently running turn in this session.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the abort has been acknowledged.</returns>
    Task AbortAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the message history for this session.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of <see cref="SquadEvent"/> messages.</returns>
    Task<IReadOnlyList<SquadEvent>> GetMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to events emitted by this session.
    /// </summary>
    /// <param name="handler">A callback invoked for each <see cref="SquadEvent"/>.</param>
    /// <returns>An <see cref="IDisposable"/> that removes the subscription when disposed.</returns>
    IDisposable On(Action<SquadEvent> handler);
}
