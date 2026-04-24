using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Abstractions;

namespace Squad.SDK.NET.Events;

/// <summary>
/// Channel-based event bus that dispatches <see cref="SquadEvent"/> instances to registered subscribers.
/// </summary>
/// <seealso cref="SquadEvent"/>
/// <seealso cref="SquadEventType"/>
public sealed class EventBus : IEventBus, IAsyncDisposable
{
    private readonly Channel<SquadEvent> _channel =
        Channel.CreateUnbounded<SquadEvent>(new UnboundedChannelOptions { SingleReader = true });

    private readonly ConcurrentDictionary<SquadEventType, ConcurrentBag<Func<SquadEvent, Task>>> _typed = new();
    private readonly ConcurrentBag<Func<SquadEvent, Task>> _allSubscribers = [];
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
        var bag = _typed.GetOrAdd(eventType, _ => []);
        bag.Add(handler);
        return new Subscription(() => RemoveHandler(bag, handler));
    }

    /// <inheritdoc />
    public IDisposable SubscribeAll(Func<SquadEvent, Task> handler)
    {
        _allSubscribers.Add(handler);
        return new Subscription(() => RemoveHandler(_allSubscribers, handler));
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
        await _dispatchLoop.ConfigureAwait(false);
    }

    private async Task DispatchLoopAsync()
    {
        await foreach (var evt in _channel.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            // Dispatch to type-specific subscribers
            if (_typed.TryGetValue(evt.Type, out var bag))
            {
                // Take snapshot to avoid concurrent modification during enumeration
                var handlers = bag.ToList();
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
            var allHandlers = _allSubscribers.ToList();
            foreach (var handler in allHandlers)
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

    private static void RemoveHandler(ConcurrentBag<Func<SquadEvent, Task>> bag, Func<SquadEvent, Task> target)
    {
        // ConcurrentBag does not support targeted removal; rebuild without the target entry.
        var remaining = bag.Where(h => h != target).ToArray();
        while (!bag.IsEmpty) bag.TryTake(out _);
        foreach (var h in remaining) bag.Add(h);
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
