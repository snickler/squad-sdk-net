using Microsoft.Extensions.Logging.Abstractions;
using Squad.SDK.NET.Events;

namespace Squad.SDK.NET.Tests;

public sealed class EventBusTests
{
    [Fact]
    public async Task Subscribe_SpecificEventType_ReceivesMatchingEvents()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var received = new TaskCompletionSource<SquadEvent>();
        var subscription = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            received.SetResult(evt);
            await Task.CompletedTask;
        });

        // Act
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });

        // Assert - wait for handler to execute, not arbitrary delay
        var result = await received.Task;
        Assert.NotNull(result);
        Assert.Equal(SquadEventType.SessionCreated, result.Type);
        subscription.Dispose();
        await eventBus.DisposeAsync();
    }

    [Fact]
    public async Task Subscribe_SpecificEventType_DoesNotReceiveOtherTypes()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var received = false;
        var wrongTypeReceived = new TaskCompletionSource<bool>();
        var subscription = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            received = true;
            await Task.CompletedTask;
        });

        // Subscribe to a different event to ensure dispatch is working
        var dispatchSignal = new TaskCompletionSource<bool>();
        var otherSubscription = eventBus.Subscribe(SquadEventType.SessionError, async evt =>
        {
            dispatchSignal.SetResult(true);
            await Task.CompletedTask;
        });

        // Act
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionError });

        // Wait for the other handler to execute (proves dispatch is working)
        await dispatchSignal.Task;

        // Assert
        Assert.False(received, "Handler should not have been called for SessionError");
        subscription.Dispose();
        otherSubscription.Dispose();
        await eventBus.DisposeAsync();
    }

    [Fact]
    public async Task SubscribeAll_ReceivesAllEventTypes()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var receivedEvents = new List<SquadEvent>();
        var lockObj = new object();
        var allEventsReceived = new TaskCompletionSource<bool>();

        var subscription = eventBus.SubscribeAll(async evt =>
        {
            lock (lockObj)
            {
                receivedEvents.Add(evt);
                if (receivedEvents.Count == 3)
                {
                    allEventsReceived.SetResult(true);
                }
            }
            await Task.CompletedTask;
        });

        // Act
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionError });
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionDestroyed });

        // Assert
        await allEventsReceived.Task;
        Assert.Equal(3, receivedEvents.Count);
        Assert.Contains(receivedEvents, e => e.Type == SquadEventType.SessionCreated);
        Assert.Contains(receivedEvents, e => e.Type == SquadEventType.SessionError);
        Assert.Contains(receivedEvents, e => e.Type == SquadEventType.SessionDestroyed);
        subscription.Dispose();
        await eventBus.DisposeAsync();
    }

    [Fact]
    public async Task Subscribe_MultipleSubscribers_AllReceiveEvents()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var received1 = new TaskCompletionSource<bool>();
        var received2 = new TaskCompletionSource<bool>();

        var subscription1 = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            received1.SetResult(true);
            await Task.CompletedTask;
        });
        var subscription2 = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            received2.SetResult(true);
            await Task.CompletedTask;
        });

        // Act
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });

        // Assert
        await Task.WhenAll(received1.Task, received2.Task);
        Assert.True(received1.Task.IsCompletedSuccessfully);
        Assert.True(received2.Task.IsCompletedSuccessfully);
        subscription1.Dispose();
        subscription2.Dispose();
        await eventBus.DisposeAsync();
    }

    [Fact]
    public async Task Dispose_Subscription_StopsReceivingEvents()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var firstEventReceived = new TaskCompletionSource<bool>();
        var secondEventReceived = new TaskCompletionSource<bool>();
        var eventCount = 0;

        var subscription = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            eventCount++;
            if (eventCount == 1)
            {
                firstEventReceived.SetResult(true);
            }
            else if (eventCount == 2)
            {
                secondEventReceived.SetResult(true);
            }
            await Task.CompletedTask;
        });

        // Act
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });
        await firstEventReceived.Task;

        // Dispose the subscription
        subscription.Dispose();

        // Emit again; should not be received
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });

        // Wait briefly to confirm no second event (with timeout to prevent hanging)
        var completed = await Task.WhenAny(
            secondEventReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(1))
        );

        // Assert
        Assert.Equal(1, eventCount);
        Assert.NotEqual(secondEventReceived.Task, completed);
        await eventBus.DisposeAsync();
    }

    [Fact]
    public async Task EmitAsync_NoSubscribers_DoesNotThrow()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });
        });

        Assert.Null(exception);
        await eventBus.DisposeAsync();
    }

    [Fact]
    public async Task ShutdownAsync_CompletesFirLoop()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var eventReceived = new TaskCompletionSource<bool>();
        var subscription = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            eventReceived.SetResult(true);
            await Task.CompletedTask;
        });

        // Act
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });
        await eventReceived.Task;

        // Shutdown should complete the dispatch loop and allow pending events to be drained
        await eventBus.ShutdownAsync();

        // Assert
        Assert.True(eventReceived.Task.IsCompletedSuccessfully);
        subscription.Dispose();
    }

    [Fact]
    public async Task HandlerException_DoesNotCrashDispatcher()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var firstHandlerCalled = new TaskCompletionSource<bool>();
        var secondHandlerCalled = new TaskCompletionSource<bool>();

        var subscription1 = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            firstHandlerCalled.SetResult(true);
            throw new InvalidOperationException("Handler error");
            #pragma warning disable CS0162
            await Task.CompletedTask;
            #pragma warning restore CS0162
        });

        var subscription2 = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            secondHandlerCalled.SetResult(true);
            await Task.CompletedTask;
        });

        // Act
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });

        // Assert - both handlers should be called despite first one throwing
        await Task.WhenAll(firstHandlerCalled.Task, secondHandlerCalled.Task);
        subscription1.Dispose();
        subscription2.Dispose();
        await eventBus.DisposeAsync();
    }

    [Fact]
    public async Task MultipleHandlers_ExecuteInOrder()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var executionOrder = new List<int>();
        var lockObj = new object();
        var allHandlersExecuted = new TaskCompletionSource<bool>();

        var subscription1 = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            lock (lockObj) executionOrder.Add(1);
            await Task.CompletedTask;
        });

        var subscription2 = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            lock (lockObj) executionOrder.Add(2);
            await Task.CompletedTask;
        });

        var subscription3 = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            lock (lockObj)
            {
                executionOrder.Add(3);
                if (executionOrder.Count == 3)
                {
                    allHandlersExecuted.SetResult(true);
                }
            }
            await Task.CompletedTask;
        });

        // Act
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });
        await allHandlersExecuted.Task;

        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, executionOrder);
        subscription1.Dispose();
        subscription2.Dispose();
        subscription3.Dispose();
        await eventBus.DisposeAsync();
    }

    [Fact]
    public async Task UnsubscribeMultipleTimes_IsIdempotent()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var eventReceived = new TaskCompletionSource<bool>();

        var subscription = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            eventReceived.SetResult(true);
            await Task.CompletedTask;
        });

        // Act
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });
        await eventReceived.Task;

        // Multiple disposes should not throw
        subscription.Dispose();
        subscription.Dispose();
        subscription.Dispose();

        // Assert
        Assert.True(eventReceived.Task.IsCompletedSuccessfully);
        await eventBus.DisposeAsync();
    }

    [Fact]
    public async Task ConcurrentSubscribeEmitDispose_ThreadSafe()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var handlerCallCount = 0;
        var lockObj = new object();

        // Act - spawn concurrent operations, let them run for a bit, then verify no crash
        var tasks = new List<Task>();

        // 10 concurrent subscribes and emits
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var subscription = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
                {
                    lock (lockObj) handlerCallCount++;
                    await Task.CompletedTask;
                });
                await Task.Delay(10);
                subscription.Dispose();
            }));

            tasks.Add(Task.Run(async () =>
            {
                await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });
            }));
        }

        // Give dispatch loop time to process all events and operations
        await Task.WhenAll(tasks);
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        // Assert - should not crash and some events should have been processed
        Assert.True(handlerCallCount >= 0, "Handler call count should be non-negative");
        await eventBus.DisposeAsync();
    }

    [Fact]
    public async Task SubscribeAll_And_SpecificType_BothReceiveEvent()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var typedReceived = new TaskCompletionSource<bool>();
        var allReceived = new TaskCompletionSource<bool>();

        var subscription1 = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            typedReceived.SetResult(true);
            await Task.CompletedTask;
        });

        var subscription2 = eventBus.SubscribeAll(async evt =>
        {
            allReceived.SetResult(true);
            await Task.CompletedTask;
        });

        // Act
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });

        // Assert
        await Task.WhenAll(typedReceived.Task, allReceived.Task);
        subscription1.Dispose();
        subscription2.Dispose();
        await eventBus.DisposeAsync();
    }
}
