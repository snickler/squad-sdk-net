using System.Collections.Concurrent;
using Squad.SDK.NET.Abstractions;

namespace Squad.SDK.NET.Runtime;

/// <summary>
/// Thread-safe pool that tracks active <see cref="ISquadSession"/> instances.
/// </summary>
public sealed class SessionPool
{
    private readonly ConcurrentDictionary<string, ISquadSession> _sessions = new();

    /// <summary>Adds a session to the pool.</summary>
    public void Add(ISquadSession session)
        => _sessions[session.SessionId] = session;

    /// <summary>Removes a session from the pool by ID.</summary>
    public void Remove(string sessionId)
        => _sessions.TryRemove(sessionId, out _);

    /// <summary>Returns the session with the given ID, or <see langword="null"/> if not found.</summary>
    public ISquadSession? Get(string sessionId)
        => _sessions.TryGetValue(sessionId, out var session) ? session : null;

    /// <summary>Returns a snapshot of all sessions currently in the pool.</summary>
    public IReadOnlyList<ISquadSession> GetAll()
        => _sessions.Values.ToList();

    /// <summary>Disposes all sessions and clears the pool.</summary>
    public async Task ShutdownAsync()
    {
        var sessions = _sessions.Values.ToArray();
        _sessions.Clear();

        foreach (var session in sessions)
        {
            await session.DisposeAsync().ConfigureAwait(false);
        }
    }
}
