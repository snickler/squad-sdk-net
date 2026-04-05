using System.Collections.Concurrent;
using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Events;

namespace Squad.SDK.NET.Runtime;

/// <summary>
/// Handles streaming events (deltas, usage, reasoning) from squad sessions and dispatches them to registered handlers.
/// </summary>
public sealed class StreamingPipeline : IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly List<IDisposable> _subscriptions = [];

    private readonly List<Func<StreamDeltaPayload, Task>> _deltaHandlers = [];
    private readonly List<Func<UsagePayload, Task>> _usageHandlers = [];
    private readonly List<Func<ReasoningDeltaPayload, Task>> _reasoningHandlers = [];

    private readonly ConcurrentDictionary<string, int> _deltaIndexes = new();
    private readonly ConcurrentDictionary<string, int> _messageCounts = new();

    private int _totalInputTokens;
    private int _totalOutputTokens;
    private decimal _totalEstimatedCost;

    /// <summary>
    /// Initializes a new <see cref="StreamingPipeline"/> and subscribes to streaming events on the given event bus.
    /// </summary>
    /// <param name="eventBus">The event bus to subscribe to for streaming events.</param>
    public StreamingPipeline(IEventBus eventBus)
    {
        _eventBus = eventBus;

        _subscriptions.Add(_eventBus.Subscribe(SquadEventType.MessageDelta, DispatchDeltaAsync));
        _subscriptions.Add(_eventBus.Subscribe(SquadEventType.Usage, DispatchUsageAsync));
        _subscriptions.Add(_eventBus.Subscribe(SquadEventType.ReasoningDelta, DispatchReasoningAsync));
    }

    /// <summary>Registers a handler invoked for each message delta event.</summary>
    /// <param name="handler">The async handler to invoke with the <see cref="StreamDeltaPayload"/>.</param>
    /// <returns>A disposable that removes the handler when disposed.</returns>
    public IDisposable OnDelta(Func<StreamDeltaPayload, Task> handler)
    {
        _deltaHandlers.Add(handler);
        return new CallbackDisposable(() => _deltaHandlers.Remove(handler));
    }

    /// <summary>Registers a handler invoked for each usage event.</summary>
    /// <param name="handler">The async handler to invoke with the <see cref="UsagePayload"/>.</param>
    /// <returns>A disposable that removes the handler when disposed.</returns>
    public IDisposable OnUsage(Func<UsagePayload, Task> handler)
    {
        _usageHandlers.Add(handler);
        return new CallbackDisposable(() => _usageHandlers.Remove(handler));
    }

    /// <summary>Registers a handler invoked for each reasoning delta event.</summary>
    /// <param name="handler">The async handler to invoke with the <see cref="ReasoningDeltaPayload"/>.</param>
    /// <returns>A disposable that removes the handler when disposed.</returns>
    public IDisposable OnReasoning(Func<ReasoningDeltaPayload, Task> handler)
    {
        _reasoningHandlers.Add(handler);
        return new CallbackDisposable(() => _reasoningHandlers.Remove(handler));
    }

    /// <summary>Resets the delta index and increments the message count for a session.</summary>
    /// <param name="sessionId">The session identifier.</param>
    public void MarkMessageStart(string sessionId)
    {
        _deltaIndexes[sessionId] = 0;
        _messageCounts.AddOrUpdate(sessionId, 1, (_, count) => count + 1);
    }

    /// <summary>Begins tracking streaming state for the specified session.</summary>
    /// <param name="sessionId">The session identifier to attach.</param>
    public void AttachToSession(string sessionId)
    {
        _deltaIndexes.TryAdd(sessionId, 0);
        _messageCounts.TryAdd(sessionId, 0);
    }

    /// <summary>Stops tracking streaming state for the specified session.</summary>
    /// <param name="sessionId">The session identifier to detach.</param>
    public void DetachFromSession(string sessionId)
    {
        _deltaIndexes.TryRemove(sessionId, out _);
        _messageCounts.TryRemove(sessionId, out _);
    }

    /// <summary>Returns an aggregated usage summary across all tracked sessions.</summary>
    /// <returns>A <see cref="UsageSummary"/> with total tokens, cost, and message count.</returns>
    public UsageSummary GetSummary() => new()
    {
        TotalInputTokens = _totalInputTokens,
        TotalOutputTokens = _totalOutputTokens,
        TotalEstimatedCost = _totalEstimatedCost,
        MessageCount = _messageCounts.Values.Sum()
    };

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var sub in _subscriptions)
            sub.Dispose();
        _subscriptions.Clear();
    }

    private async Task DispatchDeltaAsync(SquadEvent evt)
    {
        if (evt.Payload is not StreamDeltaPayload payload) return;
        foreach (var handler in _deltaHandlers)
            await handler(payload).ConfigureAwait(false);
    }

    private async Task DispatchUsageAsync(SquadEvent evt)
    {
        if (evt.Payload is not UsagePayload payload) return;

        Interlocked.Add(ref _totalInputTokens, payload.InputTokens);
        Interlocked.Add(ref _totalOutputTokens, payload.OutputTokens);

        // Accumulate estimated cost with a spin-safe decimal update
        decimal current, updated;
        do
        {
            current = _totalEstimatedCost;
            updated = current + payload.EstimatedCost;
        }
        while (Interlocked.CompareExchange(ref _totalEstimatedCost, updated, current) != current);

        foreach (var handler in _usageHandlers)
            await handler(payload).ConfigureAwait(false);
    }

    private async Task DispatchReasoningAsync(SquadEvent evt)
    {
        if (evt.Payload is not ReasoningDeltaPayload payload) return;
        foreach (var handler in _reasoningHandlers)
            await handler(payload).ConfigureAwait(false);
    }

    private sealed class CallbackDisposable(Action onDispose) : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                onDispose();
        }
    }
}

/// <summary>
/// Aggregated usage summary for the streaming pipeline.
/// </summary>
public sealed record UsageSummary
{
    /// <summary>Gets the total number of input tokens processed.</summary>
    public int TotalInputTokens { get; init; }
    /// <summary>Gets the total number of output tokens produced.</summary>
    public int TotalOutputTokens { get; init; }
    /// <summary>Gets the total estimated cost in USD.</summary>
    public decimal TotalEstimatedCost { get; init; }
    /// <summary>Gets the total number of messages processed.</summary>
    public int MessageCount { get; init; }
}
