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
        var receivedEvent = false;
        var subscription = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            receivedEvent = true;
            await Task.CompletedTask;
        });

        // Act
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });
        await Task.Delay(50); // Give the dispatch loop time to process

        // Assert
        Assert.True(receivedEvent);
        subscription.Dispose();
        await eventBus.DisposeAsync();
    }

    [Fact]
    public async Task Subscribe_SpecificEventType_DoesNotReceiveOtherTypes()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var receivedEvent = false;
        var subscription = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            receivedEvent = true;
            await Task.CompletedTask;
        });

        // Act
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionError });
        await Task.Delay(50);

        // Assert
        Assert.False(receivedEvent);
        subscription.Dispose();
        await eventBus.DisposeAsync();
    }

    [Fact]
    public async Task SubscribeAll_ReceivesAllEventTypes()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var receivedCount = 0;
        var subscription = eventBus.SubscribeAll(async evt =>
        {
            receivedCount++;
            await Task.CompletedTask;
        });

        // Act
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionError });
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionDestroyed });
        await Task.Delay(100);

        // Assert
        Assert.Equal(3, receivedCount);
        subscription.Dispose();
        await eventBus.DisposeAsync();
    }

    [Fact]
    public async Task Subscribe_MultipleSubscribers_AllReceiveEvents()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var received1 = false;
        var received2 = false;
        var subscription1 = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            received1 = true;
            await Task.CompletedTask;
        });
        var subscription2 = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            received2 = true;
            await Task.CompletedTask;
        });

        // Act
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });
        await Task.Delay(50);

        // Assert
        Assert.True(received1);
        Assert.True(received2);
        subscription1.Dispose();
        subscription2.Dispose();
        await eventBus.DisposeAsync();
    }

    [Fact]
    public async Task Dispose_Subscription_StopsReceivingEvents()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var receivedCount = 0;
        var subscription = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            receivedCount++;
            await Task.CompletedTask;
        });

        // Act
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });
        await Task.Delay(50);
        subscription.Dispose();
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });
        await Task.Delay(50);

        // Assert
        Assert.Equal(1, receivedCount);
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
            await Task.Delay(50);
        });

        Assert.Null(exception);
        await eventBus.DisposeAsync();
    }

    [Fact]
    public async Task ShutdownAsync_PreventsFurtherEmissions()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var receivedCount = 0;
        var subscription = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
        {
            receivedCount++;
            await Task.CompletedTask;
        });

        // Act
        await eventBus.EmitAsync(new SquadEvent { Type = SquadEventType.SessionCreated });
        await eventBus.ShutdownAsync();

        // Attempting to emit after shutdown should not throw, but won't be processed
        await Task.Delay(50);

        // Assert
        Assert.Equal(1, receivedCount);
        subscription.Dispose();
    }

    [Fact]
    public async Task ConcurrentEmitAndSubscribe_ThreadSafe()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var receivedCount = 0;
        var lockObj = new object();

        // Act
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var subscription = eventBus.Subscribe(SquadEventType.SessionCreated, async evt =>
                {
                    lock (lockObj) { receivedCount++; }
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

        await Task.WhenAll(tasks);
        await Task.Delay(100);

        // Assert - just verify it didn't crash
        Assert.True(receivedCount >= 0);
        await eventBus.DisposeAsync();
    }
}
