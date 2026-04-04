using Squad.SDK.NET.Events;

namespace Squad.SDK.NET.Abstractions;

/// <summary>
/// Publish-subscribe event bus for distributing <see cref="SquadEvent"/> instances within the SDK.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Subscribes to events of the specified type.
    /// </summary>
    /// <param name="eventType">The type of event to subscribe to.</param>
    /// <param name="handler">An async callback invoked when a matching event is emitted.</param>
    /// <returns>An <see cref="IDisposable"/> that removes the subscription when disposed.</returns>
    IDisposable Subscribe(SquadEventType eventType, Func<SquadEvent, Task> handler);

    /// <summary>
    /// Subscribes to all events regardless of type.
    /// </summary>
    /// <param name="handler">An async callback invoked for every emitted event.</param>
    /// <returns>An <see cref="IDisposable"/> that removes the subscription when disposed.</returns>
    IDisposable SubscribeAll(Func<SquadEvent, Task> handler);

    /// <summary>
    /// Emits an event to all matching subscribers.
    /// </summary>
    /// <param name="squadEvent">The event to emit.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when all subscribers have been notified.</returns>
    Task EmitAsync(SquadEvent squadEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuts down the event bus and releases associated resources.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when shutdown is finished.</returns>
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}
