using Squad.SDK.NET.Events;

namespace Squad.SDK.NET.Abstractions;

/// <summary>
/// Publish-subscribe event bus for distributing <see cref="SquadEvent"/> instances within the SDK.
/// </summary>
/// <remarks>
/// <para>
/// The event bus is fully thread-safe. Multiple threads may subscribe, emit events, and unsubscribe concurrently.
/// </para>
/// <para>
/// <strong>Subscription removal:</strong> Subscription removal is efficient and lock-free. Handlers can be removed
/// concurrently with event emission without blocking the dispatcher.
/// </para>
/// <para>
/// <strong>Emit semantics:</strong> <see cref="EmitAsync"/> is non-blocking. It enqueues the event and returns
/// immediately without waiting for handlers to execute. Callers must not assume handlers have completed when
/// <see cref="EmitAsync"/> returns. To ensure pending events are fully processed, call <see cref="ShutdownAsync"/>
/// before disposing the bus.
/// </para>
/// <para>
/// <strong>Handler execution:</strong> For a single event, handlers execute sequentially in subscription order
/// (type-specific handlers first, then all-type subscribers). Exceptions in one handler do not prevent subsequent
/// handlers from executing.
/// </para>
/// </remarks>
public interface IEventBus
{
    /// <summary>
    /// Subscribes to events of the specified type.
    /// </summary>
    /// <param name="eventType">The type of event to subscribe to.</param>
    /// <param name="handler">An async callback invoked when a matching event is emitted.</param>
    /// <returns>An <see cref="IDisposable"/> that removes the subscription when disposed.</returns>
    /// <remarks>
    /// The returned subscription is idempotent—disposing it multiple times is safe.
    /// </remarks>
    IDisposable Subscribe(SquadEventType eventType, Func<SquadEvent, Task> handler);

    /// <summary>
    /// Subscribes to all events regardless of type.
    /// </summary>
    /// <param name="handler">An async callback invoked for every emitted event.</param>
    /// <returns>An <see cref="IDisposable"/> that removes the subscription when disposed.</returns>
    /// <remarks>
    /// All-type subscribers execute after type-specific subscribers for each event.
    /// The returned subscription is idempotent—disposing it multiple times is safe.
    /// </remarks>
    IDisposable SubscribeAll(Func<SquadEvent, Task> handler);

    /// <summary>
    /// Enqueues an event for asynchronous dispatch to all matching subscribers.
    /// </summary>
    /// <param name="squadEvent">The event to emit.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the event is enqueued (not when handlers execute).</returns>
    /// <remarks>
    /// This method is non-blocking and returns immediately after enqueueing the event. It does not wait for
    /// handlers to complete. To ensure all pending events have been processed, call <see cref="ShutdownAsync"/>
    /// before disposing.
    /// </remarks>
    Task EmitAsync(SquadEvent squadEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuts down the event bus and waits for all pending events to be dispatched.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the dispatch loop has processed all pending events and shut down.</returns>
    /// <remarks>
    /// This method closes the event channel, allowing all pending events to drain through the dispatcher.
    /// Calling <see cref="ShutdownAsync"/> before disposal ensures that in-flight handlers have completed.
    /// </remarks>
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}
