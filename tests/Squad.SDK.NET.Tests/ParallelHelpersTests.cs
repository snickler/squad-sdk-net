using Squad.SDK.NET.Runtime;

namespace Squad.SDK.NET.Tests;

public sealed class ParallelHelpersTests
{
    [Fact]
    public async Task MapWithLimitAsync_WithItems_PreservesInputOrder()
    {
        var items = Enumerable.Range(1, 10).ToArray();

        var results = await ParallelHelpers.MapWithLimitAsync(
            items,
            3,
            static async (item, _, _) =>
            {
                await Task.Delay((11 - item) * 5);
                return item * 2;
            });

        Assert.Equal(items.Select(i => i * 2), results);
    }

    [Fact]
    public async Task MapWithLimitAsync_WithConcurrencyLimit_DoesNotExceedLimit()
    {
        var items = Enumerable.Range(0, 20).ToArray();
        var current = 0;
        var maxObserved = 0;

        await ParallelHelpers.MapWithLimitAsync(
            items,
            3,
            async (_, _, _) =>
            {
                var now = Interlocked.Increment(ref current);
                _ = InterlockedExtensions.Max(ref maxObserved, now);
                await Task.Delay(20);
                Interlocked.Decrement(ref current);
                return now;
            });

        Assert.InRange(maxObserved, 1, 3);
    }

    [Fact]
    public async Task MapWithLimitAsync_WhenMapperFails_ThrowsException()
    {
        var items = new[] { 1, 2, 3, 4 };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ParallelHelpers.MapWithLimitAsync(
                items,
                2,
                static (item, _, _) =>
                {
                    if (item == 3)
                        throw new InvalidOperationException("boom");
                    return Task.FromResult(item);
                }));

        Assert.Equal("boom", exception.Message);
    }

    [Fact]
    public async Task MapWithLimitAsync_EmptyInput_ReturnsEmptyArray()
    {
        var results = await ParallelHelpers.MapWithLimitAsync(
            Array.Empty<int>(),
            2,
            static (item, _, _) => Task.FromResult(item));

        Assert.Empty(results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task MapWithLimitAsync_InvalidLimit_ThrowsArgumentException(int limit)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            ParallelHelpers.MapWithLimitAsync(
                new[] { 1 },
                limit,
                static (item, _, _) => Task.FromResult(item)));
    }

    [Fact]
    public async Task MapWithLimitSettledAsync_WithPartialFailures_CapturesPerItemStatus()
    {
        var results = await ParallelHelpers.MapWithLimitSettledAsync(
            new[] { "ok-1", "fail", "ok-2" },
            2,
            static (item, _, _) =>
            {
                if (item == "fail")
                    throw new InvalidOperationException("failed-item");
                return Task.FromResult(item.ToUpperInvariant());
            });

        Assert.Equal(3, results.Length);

        Assert.True(results[0].Success);
        Assert.Equal("OK-1", results[0].Value);
        Assert.Null(results[0].Exception);

        Assert.False(results[1].Success);
        Assert.Null(results[1].Value);
        Assert.NotNull(results[1].Exception);
        Assert.Equal("failed-item", results[1].Exception!.Message);

        Assert.True(results[2].Success);
        Assert.Equal("OK-2", results[2].Value);
        Assert.Null(results[2].Exception);
    }

    [Fact]
    public async Task MapWithLimitSettledAsync_AllFailures_ReturnsFailureForEveryItem()
    {
        var results = await ParallelHelpers.MapWithLimitSettledAsync(
            new[] { 1, 2, 3 },
            3,
            static (_, _, _) => Task.FromException<int>(new InvalidOperationException("nope")));

        Assert.Equal(3, results.Length);
        Assert.All(results, result =>
        {
            Assert.False(result.Success);
            Assert.NotNull(result.Exception);
            Assert.Equal(default, result.Value);
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task MapWithLimitSettledAsync_InvalidLimit_ThrowsArgumentException(int limit)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            ParallelHelpers.MapWithLimitSettledAsync(
                new[] { 1 },
                limit,
                static (item, _, _) => Task.FromResult(item)));
    }

    private static class InterlockedExtensions
    {
        public static int Max(ref int location, int value)
        {
            while (true)
            {
                var current = Volatile.Read(ref location);
                if (value <= current)
                    return current;

                if (Interlocked.CompareExchange(ref location, value, current) == current)
                    return value;
            }
        }
    }
}
