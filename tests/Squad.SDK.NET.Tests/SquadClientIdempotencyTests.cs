using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Squad.SDK.NET;

namespace Squad.SDK.NET.Tests;

/// <summary>
/// Tests to verify SquadClient idempotency behavior for StartAsync/StopAsync.
/// Ensures multiple calls don't cause duplicate state changes or exceptions.
/// </summary>
public sealed class SquadClientIdempotencyTests
{
    private static SquadClient CreateClient()
    {
        return new SquadClient(NullLoggerFactory.Instance);
    }

    [Fact]
    public async Task StartAsync_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var client = CreateClient();

        // Act & Assert - Multiple starts should not throw
        await client.StartAsync();
        await client.StartAsync();
        await client.StartAsync();
        
        // If we get here without exception, the idempotency is working
        Assert.True(true);
    }

    [Fact]
    public async Task StopAsync_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var client = CreateClient();

        // Start first to ensure we can stop
        await client.StartAsync();

        // Act & Assert - Multiple stops should not throw
        await client.StopAsync();
        await client.StopAsync();
        await client.StopAsync();
        
        // If we get here without exception, the idempotency is working
        Assert.True(true);
    }

    [Fact]
    public async Task StartStopCycle_WorksCorrectly()
    {
        // Arrange
        var client = CreateClient();

        // Act & Assert - Multiple start/stop cycles should work without throwing
        await client.StartAsync();
        await client.StopAsync();
        
        await client.StartAsync();
        await client.StopAsync();
        
        await client.StartAsync();
        await client.StopAsync();
        
        // If we get here without exception, the cycles work correctly
        Assert.True(true);
    }

    [Fact]
    public async Task StopAsync_WithoutStart_DoesNotThrow()
    {
        // Arrange
        var client = CreateClient();

        // Act & Assert - Stop without start should not throw
        await client.StopAsync();
        
        // If we get here without exception, the guard is working
        Assert.True(true);
    }

    [Fact]
    public async Task DisposeAsync_WithPriorStart_DoesNotThrow()
    {
        // Arrange
        var client = CreateClient();
        await client.StartAsync();

        // Act & Assert - Dispose should work correctly
        await client.DisposeAsync();
        
        // If we get here without exception, dispose works correctly
        Assert.True(true);
    }

    [Fact]
    public async Task DisposeAsync_WithoutStart_DoesNotThrow()
    {
        // Arrange
        var client = CreateClient();

        // Act & Assert - Dispose without start should not throw
        await client.DisposeAsync();
        
        // If we get here without exception, dispose works correctly
        Assert.True(true);
    }

    [Fact]
    public async Task ConcurrentStartAsync_DoesNotThrow()
    {
        // Arrange
        var client = CreateClient();

        // Act - Run multiple StartAsync calls concurrently
        var tasks = new Task[5];
        for (int i = 0; i < 5; i++)
        {
            tasks[i] = client.StartAsync();
        }

        // Assert - All tasks should complete without exception
        await Task.WhenAll(tasks);
        
        // If we get here without exception, concurrent access is safe
        Assert.True(true);
    }
}