namespace Squad.SDK.NET.Storage;

/// <summary>
/// Provides asynchronous key-value storage operations.
/// </summary>
public interface IStorageProvider
{
    /// <summary>Reads the value for the given key.</summary>
    /// <param name="key">The storage key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored value, or <see langword="null"/> if the key does not exist.</returns>
    Task<string?> ReadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Writes a value for the given key, creating or overwriting it.</summary>
    /// <param name="key">The storage key.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>Checks whether the given key exists in storage.</summary>
    /// <param name="key">The storage key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> if the key exists; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Deletes the value for the given key, if it exists.</summary>
    /// <param name="key">The storage key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Lists all keys matching the given prefix.</summary>
    /// <param name="prefix">The key prefix to filter by; empty returns all keys.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of matching keys.</returns>
    Task<IReadOnlyList<string>> ListAsync(string prefix = "", CancellationToken cancellationToken = default);

    /// <summary>Returns storage statistics such as item count and total size.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="StorageStats"/> snapshot.</returns>
    Task<StorageStats> GetStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics about the current state of a storage provider.
/// </summary>
public sealed record StorageStats
{
    /// <summary>Gets the number of stored items.</summary>
    public int ItemCount { get; init; }
    /// <summary>Gets the total size in bytes.</summary>
    public long TotalSizeBytes { get; init; }
    /// <summary>Gets the timestamp of the most recently modified item.</summary>
    public DateTimeOffset? LastModified { get; init; }
}
