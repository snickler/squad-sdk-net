namespace Squad.SDK.NET.Storage;

public interface IStorageProvider
{
    Task<string?> ReadAsync(string key, CancellationToken cancellationToken = default);
    Task WriteAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ListAsync(string prefix = "", CancellationToken cancellationToken = default);
    Task<StorageStats> GetStatsAsync(CancellationToken cancellationToken = default);
}

public sealed record StorageStats
{
    public int ItemCount { get; init; }
    public long TotalSizeBytes { get; init; }
    public DateTimeOffset? LastModified { get; init; }
}
