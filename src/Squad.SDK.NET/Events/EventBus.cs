using System.Collections.Immutable;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Abstractions;

namespace Squad.SDK.NET.Events;

/// <summary>
/// Channel-based event bus that dispatches <see cref="SquadEvent"/> instances to registered subscribers.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Thread-safety:</strong> This implementation is fully thread-safe. Multiple threads may subscribe,
/// emit events, and unsubscribe concurrently without synchronization.
/// </para>
/// <para>
/// <strong>Subscription removal:</strong> Subscription removal is O(1) logical time. The bus maintains immutable
/// snapshots of handler lists, enabling efficient concurrent modifications and event dispatch without blocking.
/// </para>
/// <para>
/// <strong>Event dispatch semantics:</strong> <see cref="EmitAsync"/> is non-blocking and returns immediately after
/// enqueueing the event. Handlers are executed asynchronously by the internal dispatch loop. Callers must not
/// assume handlers have completed when <see cref="EmitAsync"/> returns.
/// </para>
/// <para>
/// <strong>Handler execution order:</strong> For a single event, type-specific handlers execute sequentially in
/// subscription order, followed by all-type subscribers (also in subscription order). However, concurrent events
/// may have handlers that execute concurrently with handlers from other events.
/// </para>
/// <para>
/// <strong>Disposal guarantees:</strong> <see cref="DisposeAsync"/> does not wait for in-flight handlers to
/// complete. Call <see cref="ShutdownAsync"/> first to drain pending events and allow handlers to finish.
/// </para>
/// </remarks>
/// <seealso cref="SquadEvent"/>
/// <seealso cref="SquadEventType"/>
public sealed class EventBus : IEventBus, IAsyncDisposable
{
    // NOTE: SingleReader=true signals the channel is only read by the DispatchLoopAsync method.
    // This is a contract guarantee and must not be violated by adding additional readers.
    private readonly Channel<SquadEvent> _channel =
        Channel.CreateUnbounded<SquadEvent>(new UnboundedChannelOptions { SingleReader = true });

    // Immutable collections allow O(1) logical removal and efficient snapshots without per-event allocations.
    // ReaderWriterLockSlim ensures hot-path reads (dispatch) are lock-free, writes (subscribe/unsubscribe) are serialized.
    private ImmutableDictionary<SquadEventType, ImmutableList<Func<SquadEvent, Task>>> _typed =
        ImmutableDictionary<SquadEventType, ImmutableList<Func<SquadEvent, Task>>>.Empty;

    private ImmutableList<Func<SquadEvent, Task>> _allSubscribers =
        ImmutableList<Func<SquadEvent, Task>>.Empty;

    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Task _dispatchLoop;
    private readonly ILogger<EventBus> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBus"/> class and starts the internal dispatch loop.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is <see langword="null"/>.</exception>
    public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dispatchLoop = Task.Run(DispatchLoopAsync);
    }

    /// <inheritdoc />
    public IDisposable Subscribe(SquadEventType eventType, Func<SquadEvent, Task> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        _lock.EnterWriteLock();
        try
        {
            var oldList = _typed.TryGetValue(eventType, out var existing)
                ? existing
                : ImmutableList<Func<SquadEvent, Task>>.Empty;
            var newList = oldList.Add(handler);
            _typed = _typed.SetItem(eventType, newList);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return new Subscription(() => RemoveHandler(eventType, handler));
    }

    /// <inheritdoc />
    public IDisposable SubscribeAll(Func<SquadEvent, Task> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        _lock.EnterWriteLock();
        try
        {
            _allSubscribers = _allSubscribers.Add(handler);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return new Subscription(() => RemoveFromAll(handler));
    }

    /// <inheritdoc />
    public async Task EmitAsync(SquadEvent squadEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Event emitted: {Type} session={SessionId}", squadEvent.Type, squadEvent.SessionId);
        await _channel.Writer.WriteAsync(squadEvent, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _channel.Writer.TryComplete();
        await _dispatchLoop.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Completes the event channel and waits for the dispatch loop to drain.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        try
        {
            await _dispatchLoop.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Dispatch loop was cancelled; this is acceptable during shutdown.
        }
        finally
        {
            _lock.Dispose();
        }
    }

    private async Task DispatchLoopAsync()
    {
        await foreach (var evt in _channel.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            // Dispatch to type-specific subscribers (with read lock for safety)
            _lock.EnterReadLock();
            ImmutableDictionary<SquadEventType, ImmutableList<Func<SquadEvent, Task>>> typed;
            ImmutableList<Func<SquadEvent, Task>> allSubscribers;
            try
            {
                typed = _typed;
                allSubscribers = _allSubscribers;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            // Dispatch to type-specific subscribers (outside the lock)
            if (typed.TryGetValue(evt.Type, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        await handler(evt).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Event handler failed for {Type}", evt.Type);
                    }
                }
            }

            // Dispatch to all-subscribers
            foreach (var handler in allSubscribers)
            {
                try
                {
                    await handler(evt).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Event handler failed for all-subscriber");
                }
            }
        }
    }

    private void RemoveHandler(SquadEventType eventType, Func<SquadEvent, Task> target)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_typed.TryGetValue(eventType, out var oldList))
                return; // Already removed or never existed.

            var newList = oldList.Remove(target);
            if (newList.Count == 0)
            {
                _typed = _typed.Remove(eventType);
            }
            else
            {
                _typed = _typed.SetItem(eventType, newList);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void RemoveFromAll(Func<SquadEvent, Task> target)
    {
        _lock.EnterWriteLock();
        try
        {
            _allSubscribers = _allSubscribers.Remove(target);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private sealed class Subscription(Action onDispose) : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                onDispose();
        }
    }
}
