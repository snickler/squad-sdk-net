using Squad.SDK.NET.Events;

namespace Squad.SDK.NET.Abstractions;

/// <summary>
/// High-level client for managing a squad of AI agents.
/// Wraps CopilotClient with multi-session orchestration.
/// </summary>
public interface ISquadClient : IAsyncDisposable
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task<ISquadSession> CreateSessionAsync(SquadSessionConfig? config = null, CancellationToken cancellationToken = default);
    Task<ISquadSession> ResumeSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SquadSessionMetadata>> ListSessionsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Lists available AI models with their capabilities.
    /// </summary>
    Task<IReadOnlyList<SquadModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default);
    ConnectionState State { get; }
    IDisposable On(Action<SquadEvent> handler);
}
