using System.Collections.Concurrent;

namespace Squad.SDK.NET.Storage;

/// <summary>
/// In-memory implementation of <see cref="IStorageProvider"/> backed by a <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>.
/// </summary>
public sealed class InMemoryStorageProvider : IStorageProvider
{
    private readonly ConcurrentDictionary<string, string> _store = new();

    /// <inheritdoc />
    public Task<string?> ReadAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    /// <inheritdoc />
    public Task WriteAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        _store[key] = value;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.ContainsKey(key));
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListAsync(string prefix = "", CancellationToken cancellationToken = default)
    {
        var keys = _store.Keys
            .Where(k => string.IsNullOrEmpty(prefix) || k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Order()
            .ToList();
        return Task.FromResult<IReadOnlyList<string>>(keys.AsReadOnly());
    }

    /// <inheritdoc />
    public Task<StorageStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var totalSize = _store.Values.Sum(v => (long)System.Text.Encoding.UTF8.GetByteCount(v));
        return Task.FromResult(new StorageStats
        {
            ItemCount = _store.Count,
            TotalSizeBytes = totalSize,
            LastModified = _store.Count > 0 ? DateTimeOffset.UtcNow : null
        });
    }
}
