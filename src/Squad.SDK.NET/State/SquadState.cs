using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Squad.SDK.NET.Storage;

namespace Squad.SDK.NET.State;

public sealed class SquadState
{
    private readonly IStorageProvider _storage;

    public SquadState(IStorageProvider storage)
    {
        _storage = storage;
        Agents = new TypedCollection<AgentEntity>(_storage, "agents", SquadStateJsonContext.Default.AgentEntity, SquadStateJsonContext.Default.IReadOnlyListAgentEntity);
        Decisions = new TypedCollection<Decision>(_storage, "decisions", SquadStateJsonContext.Default.Decision, SquadStateJsonContext.Default.IReadOnlyListDecision);
        History = new TypedCollection<HistoryEntry>(_storage, "history", SquadStateJsonContext.Default.HistoryEntry, SquadStateJsonContext.Default.IReadOnlyListHistoryEntry);
        Logs = new TypedCollection<LogEntry>(_storage, "logs", SquadStateJsonContext.Default.LogEntry, SquadStateJsonContext.Default.IReadOnlyListLogEntry);
    }

    public TypedCollection<AgentEntity> Agents { get; }
    public TypedCollection<Decision> Decisions { get; }
    public TypedCollection<HistoryEntry> History { get; }
    public TypedCollection<LogEntry> Logs { get; }
}

public sealed class TypedCollection<T>
{
    private readonly IStorageProvider _storage;
    private readonly string _prefix;
    private readonly JsonTypeInfo<T> _itemTypeInfo;
    private readonly JsonTypeInfo<IReadOnlyList<T>> _listTypeInfo;

    public TypedCollection(IStorageProvider storage, string prefix, JsonTypeInfo<T> itemTypeInfo, JsonTypeInfo<IReadOnlyList<T>> listTypeInfo)
    {
        _storage = storage;
        _prefix = prefix;
        _itemTypeInfo = itemTypeInfo;
        _listTypeInfo = listTypeInfo;
    }

    public async Task<T?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var json = await _storage.ReadAsync($"{_prefix}/{key}.json", cancellationToken);
        if (json is null) return default;
        return JsonSerializer.Deserialize(json, _itemTypeInfo);
    }

    public async Task SetAsync(string key, T value, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, _itemTypeInfo);
        await _storage.WriteAsync($"{_prefix}/{key}.json", json, cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await _storage.DeleteAsync($"{_prefix}/{key}.json", cancellationToken);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _storage.ExistsAsync($"{_prefix}/{key}.json", cancellationToken);
    }

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
