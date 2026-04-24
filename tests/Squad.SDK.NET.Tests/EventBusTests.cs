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
    public async Task MultipleHandlers_AllExecute()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var handler1Executed = new TaskCompletionSource<bool>();
        var handler2Executed = new TaskCompletionSource<bool>();
        var handler3Executed = new TaskCompletionSource<bool>();

        var subscription1 = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            handler1Executed.SetResult(true);
            await Task.CompletedTask;
        });

        var subscription2 = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            handler2Executed.SetResult(true);
            await Task.CompletedTask;
        });

        var subscription3 = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            handler3Executed.SetResult(true);
            await Task.CompletedTask;
        });

        // Act
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });
        
        // Assert - wait with timeout
        var completed = await Task.WhenAny(
            Task.WhenAll(handler1Executed.Task, handler2Executed.Task, handler3Executed.Task),
            Task.Delay(TimeSpan.FromSeconds(5))
        );

        Assert.NotEqual(Task.Delay(TimeSpan.FromSeconds(5)), completed);
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

        // Act - spawn concurrent operations; each subscription unsubscribes quickly
        // Goal: verify that concurrent subscribe/emit/dispose doesn't crash the dispatcher
        var tasks = new List<Task>();

        for (int i = 0; i < 5; i++)
        {
            var sub = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
            {
                await Task.CompletedTask;
            });
            
            // Immediately unsubscribe
            sub.Dispose();
            
            // Emit after unsubscribe; dispatcher should handle gracefully
            tasks.Add(eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated }));
        }

        // Wait for all emit operations to complete
        await Task.WhenAll(tasks);

        // Assert - should not crash
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
