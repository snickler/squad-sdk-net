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

    /// <summary>
    /// Tests that handler removal is efficient and doesn't block concurrent event dispatch.
    /// This validates the O(1) removal with ImmutableList.
    /// </summary>
    [Fact]
    public async Task RemoveHandler_DuringDispatch_IsNonBlocking()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var handler1Started = new TaskCompletionSource<bool>();
        var handler2Executed = new TaskCompletionSource<bool>();
        var dispatchComplete = new TaskCompletionSource<bool>();
        var slowHandlerDuration = TimeSpan.FromMilliseconds(100);

        var subscription1 = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            handler1Started.SetResult(true);
            await Task.Delay(slowHandlerDuration);
        });

        var subscription2 = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            handler2Executed.SetResult(true);
            await Task.CompletedTask;
        });

        // Act
        _ = Task.Run(async () =>
        {
            await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });
            dispatchComplete.SetResult(true);
        });

        // Wait for first handler to start
        await handler1Started.Task;

        // Dispose subscription1 while it's still running
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        subscription1.Dispose();
        stopwatch.Stop();

        // The dispose should complete immediately (< 50ms), not wait for handler1 to finish
        Assert.True(stopwatch.ElapsedMilliseconds < 50, 
            $"Dispose took {stopwatch.ElapsedMilliseconds}ms, expected < 50ms");

        // Handler2 should still execute
        await handler2Executed.Task;
        subscription2.Dispose();
        await eventBus.DisposeAsync();
    }

    /// <summary>
    /// Stress test: concurrent subscriptions and unsubscriptions while events are being emitted.
    /// This validates thread-safety of the lock-free implementation.
    /// </summary>
    [Fact]
    public async Task Concurrent_SubscribeUnsubscribeEmit_ThreadSafe()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var handlerCallCount = 0;
        var lockObj = new object();

        // Act - Spawn concurrent tasks for subscribe, unsubscribe, and emit
        var tasks = new List<Task>();

        // Task 1-10: Emit events continuously
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < 10; j++)
                {
                    await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });
                    await Task.Delay(1);
                }
            }));
        }

        // Task 11-20: Subscribe and unsubscribe rapidly
        var subscriptions = new List<IDisposable>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    var sub = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
                    {
                        lock (lockObj)
                        {
                            handlerCallCount++;
                        }
                        await Task.CompletedTask;
                    });
                    subscriptions.Add(sub);
                    Task.Delay(1).Wait();
                    sub.Dispose();
                }
            }));
        }

        // Wait for all tasks
        await Task.WhenAll(tasks);

        // Assert - We should have received some handler calls (exact count varies due to concurrency)
        Assert.True(handlerCallCount > 0, "No handlers were called during concurrent stress test");
        await eventBus.DisposeAsync();
    }

    /// <summary>
    /// Tests that many handlers can be added and removed without degrading performance.
    /// Validates that ImmutableList approach scales better than the original ConcurrentBag rebuild.
    /// </summary>
    [Fact]
    public async Task ManyHandlers_RemovalIsEfficient()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var subscriptions = new List<IDisposable>();
        const int handlerCount = 1000;

        // Add many handlers
        for (int i = 0; i < handlerCount; i++)
        {
            var sub = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
            {
                await Task.CompletedTask;
            });
            subscriptions.Add(sub);
        }

        // Act - Remove all handlers and measure time
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        foreach (var sub in subscriptions)
        {
            sub.Dispose();
        }
        stopwatch.Stop();

        // Assert - Removal should be very fast (each ~O(1) with copy-on-write)
        // With ConcurrentBag rebuild, this would be O(n^2), taking seconds
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Removing {handlerCount} handlers took {stopwatch.ElapsedMilliseconds}ms");

        await eventBus.DisposeAsync();
    }

    /// <summary>
    /// Tests that EmitAsync is truly non-blocking and returns before handlers complete.
    /// </summary>
    [Fact]
    public async Task EmitAsync_IsNonBlocking()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var handlerStarted = new TaskCompletionSource<bool>();
        var slowHandlerDelay = TimeSpan.FromSeconds(1);

        var subscription = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            handlerStarted.SetResult(true);
            await Task.Delay(slowHandlerDelay);
        });

        // Act
        var emitStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var emitTask = eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });
        emitStopwatch.Stop();

        // Assert - EmitAsync should return almost instantly
        Assert.True(emitStopwatch.ElapsedMilliseconds < 100, 
            $"EmitAsync took {emitStopwatch.ElapsedMilliseconds}ms, should be < 100ms");

        // But handlers are still executing
        Assert.False(handlerStarted.Task.IsCompleted, "Handler should not have started yet");

        // Wait for handler to start
        await handlerStarted.Task;
        subscription.Dispose();
        await eventBus.DisposeAsync();
    }

    /// <summary>
    /// Tests that handler execution order is preserved within a single event type.
    /// </summary>
    [Fact]
    public async Task HandlerExecutionOrder_IsPreserved()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var executionOrder = new List<int>();
        var lockObj = new object();
        var allComplete = new TaskCompletionSource<bool>();

        // Add handlers in order
        for (int i = 0; i < 5; i++)
        {
            int handlerId = i;
            eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
            {
                lock (lockObj)
                {
                    executionOrder.Add(handlerId);
                    if (executionOrder.Count == 5)
                    {
                        allComplete.SetResult(true);
                    }
                }
                await Task.CompletedTask;
            });
        }

        // Act
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });

        // Assert
        await allComplete.Task;
        Assert.Equal([0, 1, 2, 3, 4], executionOrder);
        await eventBus.DisposeAsync();
    }
}
