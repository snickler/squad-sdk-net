using Squad.SDK.NET.Events;

namespace Squad.SDK.NET.Abstractions;

public interface ISquadSession : IAsyncDisposable
{
    string SessionId { get; }
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

    Task AbortAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SquadEvent>> GetMessagesAsync(CancellationToken cancellationToken = default);
    IDisposable On(Action<SquadEvent> handler);
}
