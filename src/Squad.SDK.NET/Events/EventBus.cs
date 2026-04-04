using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Abstractions;

namespace Squad.SDK.NET.Events;

public sealed class EventBus : IEventBus, IAsyncDisposable
{
    private readonly Channel<SquadEvent> _channel =
        Channel.CreateUnbounded<SquadEvent>(new UnboundedChannelOptions { SingleReader = true });

    private readonly ConcurrentDictionary<SquadEventType, ConcurrentBag<Func<SquadEvent, Task>>> _typed = new();
    private readonly ConcurrentBag<Func<SquadEvent, Task>> _allSubscribers = [];
    private readonly Task _dispatchLoop;
    private readonly ILogger<EventBus> _logger;

    public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dispatchLoop = Task.Run(DispatchLoopAsync);
    }

    public IDisposable Subscribe(SquadEventType eventType, Func<SquadEvent, Task> handler)
    {
        var bag = _typed.GetOrAdd(eventType, _ => []);
        bag.Add(handler);
        return new Subscription(() => RemoveHandler(bag, handler));
    }

    public IDisposable SubscribeAll(Func<SquadEvent, Task> handler)
    {
        _allSubscribers.Add(handler);
        return new Subscription(() => RemoveHandler(_allSubscribers, handler));
    }

    public async Task EmitAsync(SquadEvent squadEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Event emitted: {Type} session={SessionId}", squadEvent.Type, squadEvent.SessionId);
        await _channel.Writer.WriteAsync(squadEvent, cancellationToken).ConfigureAwait(false);
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _channel.Writer.TryComplete();
        await _dispatchLoop.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

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
                foreach (var handler in bag)
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
            foreach (var handler in _allSubscribers)
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
