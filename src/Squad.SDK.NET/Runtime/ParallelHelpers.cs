namespace Squad.SDK.NET.Runtime;

/// <summary>
/// Bounded-concurrency helper for fan-out async work.
/// </summary>
public static class ParallelHelpers
{
    /// <summary>
    /// Run <paramref name="fn"/> against each item with at most <paramref name="limit"/> operations in flight.
    /// </summary>
    /// <remarks>
    /// Results are returned in <b>input order</b>, regardless of the order in which individual promises settle.
    /// This matches the semantics callers usually want when migrating from a sequential
    /// <c>foreach (var x in xs) { result.Add(await fn(x)); }</c> pattern: ordering is preserved, but throughput is bounded.
    /// <para>
    /// Errors propagate via the returned Task. Use <see cref="MapWithLimitSettledAsync{T,TResult}"/> when individual
    /// failures should not abort the batch.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">Input item type.</typeparam>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <param name="items">Inputs to map over.</param>
    /// <param name="limit">Maximum concurrent calls (must be ≥ 1).</param>
    /// <param name="fn">Async mapper function.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Array of results in the same order as <paramref name="items"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="limit"/> is less than 1.</exception>
    /// <example>
    /// <code>
    /// // 8 charters fetched 5-at-a-time over HTTP:
    /// var manifests = await ParallelHelpers.MapWithLimitAsync(dirs, 5, FetchCharterAsync);
    /// </code>
    /// </example>
    public static async Task<TResult[]> MapWithLimitAsync<T, TResult>(
        IReadOnlyList<T> items,
        int limit,
        Func<T, int, CancellationToken, Task<TResult>> fn,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1 || !int.IsPositive(limit))
            throw new ArgumentException($"Limit must be a positive integer, got {limit}", nameof(limit));

        if (items.Count == 0)
            return [];

        var results = new TResult[items.Count];
        var nextIndex = 0;
        var workerCount = Math.Min(limit, items.Count);

        async Task WorkerAsync()
        {
            while (true)
            {
                var idx = Interlocked.Increment(ref nextIndex) - 1;
                if (idx >= items.Count)
                    return;

                results[idx] = await fn(items[idx], idx, cancellationToken).ConfigureAwait(false);
            }
        }

        var workers = new Task[workerCount];
        for (var i = 0; i < workerCount; i++)
            workers[i] = WorkerAsync();

        await Task.WhenAll(workers).ConfigureAwait(false);
        return results;
    }

    /// <summary>
    /// Variant of <see cref="MapWithLimitAsync{T,TResult}"/> that captures individual failures
    /// rather than aborting on the first rejection.
    /// </summary>
    /// <remarks>
    /// Returns an array of results where each element is either a success (<c>Success = true</c>, <c>Value</c> set)
    /// or failure (<c>Success = false</c>, <c>Exception</c> set) in input order.
    /// <para>
    /// Use this when one bad input (e.g. a corrupt charter.md) should not stop the whole batch.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">Input item type.</typeparam>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <param name="items">Inputs to map over.</param>
    /// <param name="limit">Maximum concurrent calls (must be ≥ 1).</param>
    /// <param name="fn">Async mapper function.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Array of <see cref="SettledResult{TResult}"/> in the same order as <paramref name="items"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="limit"/> is less than 1.</exception>
    public static async Task<SettledResult<TResult>[]> MapWithLimitSettledAsync<T, TResult>(
        IReadOnlyList<T> items,
        int limit,
        Func<T, int, CancellationToken, Task<TResult>> fn,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1 || !int.IsPositive(limit))
            throw new ArgumentException($"Limit must be a positive integer, got {limit}", nameof(limit));

        if (items.Count == 0)
            return [];

        var results = new SettledResult<TResult>[items.Count];
        var nextIndex = 0;
        var workerCount = Math.Min(limit, items.Count);

        async Task WorkerAsync()
        {
            while (true)
            {
                var idx = Interlocked.Increment(ref nextIndex) - 1;
                if (idx >= items.Count)
                    return;

                try
                {
                    var value = await fn(items[idx], idx, cancellationToken).ConfigureAwait(false);
                    results[idx] = SettledResult<TResult>.FromSuccess(value);
                }
                catch (Exception ex)
                {
                    results[idx] = SettledResult<TResult>.FromFailure(ex);
                }
            }
        }

        var workers = new Task[workerCount];
        for (var i = 0; i < workerCount; i++)
            workers[i] = WorkerAsync();

        await Task.WhenAll(workers).ConfigureAwait(false);
        return results;
    }
}

/// <summary>
/// Represents the result of a settled asynchronous operation.
/// </summary>
/// <typeparam name="T">The type of the successful result value.</typeparam>
public sealed record SettledResult<T>
{
    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public required bool Success { get; init; }

    /// <summary>Gets the successful result value (only valid when <see cref="Success"/> is <see langword="true"/>).</summary>
    public T? Value { get; init; }

    /// <summary>Gets the exception that occurred (only valid when <see cref="Success"/> is <see langword="false"/>).</summary>
    public Exception? Exception { get; init; }

    /// <summary>Creates a settled result representing a successful operation.</summary>
    /// <param name="value">The result value.</param>
    /// <returns>A <see cref="SettledResult{T}"/> with <see cref="Success"/> set to <see langword="true"/>.</returns>
    public static SettledResult<T> FromSuccess(T value) => new() { Success = true, Value = value };

    /// <summary>Creates a settled result representing a failed operation.</summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A <see cref="SettledResult{T}"/> with <see cref="Success"/> set to <see langword="false"/>.</returns>
    public static SettledResult<T> FromFailure(Exception exception) => new() { Success = false, Exception = exception };
}
