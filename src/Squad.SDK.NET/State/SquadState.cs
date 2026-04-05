using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Squad.SDK.NET.Storage;

namespace Squad.SDK.NET.State;

/// <summary>
/// Provides strongly-typed, JSON-backed state collections persisted through an <see cref="IStorageProvider"/>.
/// </summary>
public sealed class SquadState
{
    private readonly IStorageProvider _storage;

    /// <summary>
    /// Initializes a new <see cref="SquadState"/> with the given storage provider.
    /// </summary>
    /// <param name="storage">The storage provider used to persist state.</param>
    public SquadState(IStorageProvider storage)
    {
        _storage = storage;
        Agents = new TypedCollection<AgentEntity>(_storage, "agents", SquadStateJsonContext.Default.AgentEntity, SquadStateJsonContext.Default.IReadOnlyListAgentEntity);
        Decisions = new TypedCollection<Decision>(_storage, "decisions", SquadStateJsonContext.Default.Decision, SquadStateJsonContext.Default.IReadOnlyListDecision);
        History = new TypedCollection<HistoryEntry>(_storage, "history", SquadStateJsonContext.Default.HistoryEntry, SquadStateJsonContext.Default.IReadOnlyListHistoryEntry);
        Logs = new TypedCollection<LogEntry>(_storage, "logs", SquadStateJsonContext.Default.LogEntry, SquadStateJsonContext.Default.IReadOnlyListLogEntry);
    }

    /// <summary>Gets the collection of agent entities.</summary>
    public TypedCollection<AgentEntity> Agents { get; }
    /// <summary>Gets the collection of architectural decisions.</summary>
    public TypedCollection<Decision> Decisions { get; }
    /// <summary>Gets the collection of history entries.</summary>
    public TypedCollection<HistoryEntry> History { get; }
    /// <summary>Gets the collection of log entries.</summary>
    public TypedCollection<LogEntry> Logs { get; }
}

/// <summary>
/// A typed, JSON-serialized collection backed by an <see cref="IStorageProvider"/>.
/// </summary>
/// <typeparam name="T">The entity type stored in this collection.</typeparam>
public sealed class TypedCollection<T>
{
    private readonly IStorageProvider _storage;
    private readonly string _prefix;
    private readonly JsonTypeInfo<T> _itemTypeInfo;
    private readonly JsonTypeInfo<IReadOnlyList<T>> _listTypeInfo;

    /// <summary>
    /// Initializes a new <see cref="TypedCollection{T}"/>.
    /// </summary>
    /// <param name="storage">The underlying storage provider.</param>
    /// <param name="prefix">Key prefix for this collection.</param>
    /// <param name="itemTypeInfo">JSON type info for individual items.</param>
    /// <param name="listTypeInfo">JSON type info for lists of items.</param>
    public TypedCollection(IStorageProvider storage, string prefix, JsonTypeInfo<T> itemTypeInfo, JsonTypeInfo<IReadOnlyList<T>> listTypeInfo)
    {
        _storage = storage;
        _prefix = prefix;
        _itemTypeInfo = itemTypeInfo;
        _listTypeInfo = listTypeInfo;
    }

    /// <summary>Gets an entity by key, or <see langword="null"/> if not found.</summary>
    /// <param name="key">The entity key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized entity, or the default value if not found.</returns>
    public async Task<T?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var json = await _storage.ReadAsync($"{_prefix}/{key}.json", cancellationToken);
        if (json is null) return default;
        return JsonSerializer.Deserialize(json, _itemTypeInfo);
    }

    /// <summary>Stores an entity under the specified key.</summary>
    /// <param name="key">The entity key.</param>
    /// <param name="value">The entity to serialize and store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SetAsync(string key, T value, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, _itemTypeInfo);
        await _storage.WriteAsync($"{_prefix}/{key}.json", json, cancellationToken);
    }

    /// <summary>Deletes the entity with the specified key.</summary>
    /// <param name="key">The entity key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await _storage.DeleteAsync($"{_prefix}/{key}.json", cancellationToken);
    }

    /// <summary>Returns <see langword="true"/> if an entity with the given key exists.</summary>
    /// <param name="key">The entity key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the entity exists.</returns>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _storage.ExistsAsync($"{_prefix}/{key}.json", cancellationToken);
    }

    /// <summary>Lists all entity keys in this collection.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of entity keys (without file extensions).</returns>
    public async Task<IReadOnlyList<string>> ListKeysAsync(CancellationToken cancellationToken = default)
    {
        var keys = await _storage.ListAsync($"{_prefix}/", cancellationToken);
        return keys
            .Where(k => k.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .Select(k => Path.GetFileNameWithoutExtension(k[((_prefix.Length + 1))..]))
            .ToList()
            .AsReadOnly();
    }
}
