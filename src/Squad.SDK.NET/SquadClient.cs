using GitHub.Copilot.SDK;
using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Events;
using SdkConnectionState = GitHub.Copilot.SDK.ConnectionState;

namespace Squad.SDK.NET;

/// <summary>
/// Wraps <see cref="CopilotClient"/> with multi-session orchestration for a squad of agents.
/// </summary>
public sealed class SquadClient : ISquadClient
{
    private readonly CopilotClient _copilotClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<SquadClient> _logger;
    private bool _isStarted;

    public SquadClient(ILoggerFactory loggerFactory, CopilotClientOptions? options = null)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = _loggerFactory.CreateLogger<SquadClient>();
        _copilotClient = new CopilotClient(options ?? new CopilotClientOptions());
    }

    public Abstractions.ConnectionState State
    {
        get
        {
            var state = MapConnectionState(_copilotClient.State);
            _logger.LogDebug("Client state: {State}", state);
            return state;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isStarted)
        {
            _logger.LogDebug("SquadClient already started, skipping");
            return;
        }

        _logger.LogInformation("Starting SquadClient");
        await _copilotClient.StartAsync(cancellationToken);
        _isStarted = true;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStarted)
        {
            _logger.LogDebug("SquadClient already stopped, skipping");
            return;
        }

        _logger.LogInformation("Stopping SquadClient");
        await _copilotClient.StopAsync();
        _isStarted = false;
    }

    public async Task<ISquadSession> CreateSessionAsync(
        SquadSessionConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating session '{ClientName}' with model '{Model}'", 
                config?.ClientName ?? "default", config?.Model ?? "default");
            
            var sdkConfig = MapSessionConfig(config);
            var session = await _copilotClient.CreateSessionAsync(sdkConfig, cancellationToken);
            var sessionLogger = _loggerFactory.CreateLogger<SquadSession>();
            return new SquadSession(session, sessionLogger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create session");
            throw;
        }
    }

    public async Task<ISquadSession> ResumeSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resuming session {SessionId}", sessionId);
        var session = await _copilotClient.ResumeSessionAsync(
            sessionId,
            new ResumeSessionConfig(),
            cancellationToken);
        var sessionLogger = _loggerFactory.CreateLogger<SquadSession>();
        return new SquadSession(session, sessionLogger);
    }

    public Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        => _copilotClient.DeleteSessionAsync(sessionId, cancellationToken);

    public async Task<IReadOnlyList<SquadSessionMetadata>> ListSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        var sessions = await _copilotClient.ListSessionsAsync(new SessionListFilter(), cancellationToken);
        return sessions
            .Select(s => new SquadSessionMetadata
            {
                SessionId = s.SessionId,
                CreatedAt = s.StartTime,
                LastActiveAt = s.ModifiedTime,
                AgentName = s.Summary
            })
            .ToList();
    }

    public async Task<IReadOnlyList<SquadModelInfo>> ListModelsAsync(
        CancellationToken cancellationToken = default)
    {
        var models = await _copilotClient.ListModelsAsync(cancellationToken);
        var result = models
            .Select(m => new SquadModelInfo
            {
                Id = m.Id,
                Name = m.Name,
                SupportedReasoningEfforts = m.SupportedReasoningEfforts?.AsReadOnly(),
                DefaultReasoningEffort = m.DefaultReasoningEffort
            })
            .ToList();
        _logger.LogDebug("Listed {Count} available models", result.Count);
        return result;
    }

    public IDisposable On(Action<SquadEvent> handler)
    {
        return _copilotClient.On(evt =>
        {
            handler(new SquadEvent
            {
                Type = MapLifecycleEventType(evt.Type),
                SessionId = evt.SessionId,
                Timestamp = DateTimeOffset.UtcNow
            });
        });
    }

    public async ValueTask DisposeAsync()
    {
        _isStarted = false;
        await _copilotClient.DisposeAsync();
    }

    private static Abstractions.ConnectionState MapConnectionState(SdkConnectionState state) => state switch
    {
        SdkConnectionState.Disconnected => Abstractions.ConnectionState.Disconnected,
        SdkConnectionState.Connecting   => Abstractions.ConnectionState.Connecting,
        SdkConnectionState.Connected    => Abstractions.ConnectionState.Connected,
        SdkConnectionState.Error        => Abstractions.ConnectionState.Error,
        _                               => Abstractions.ConnectionState.Disconnected
    };

    private static SquadEventType MapLifecycleEventType(string? type) => type switch
    {
        SessionLifecycleEventTypes.Created => SquadEventType.SessionCreated,
        SessionLifecycleEventTypes.Deleted => SquadEventType.SessionDestroyed,
        _                                  => SquadEventType.SessionCreated
    };

    private static SessionConfig MapSessionConfig(SquadSessionConfig? config)
    {
        if (config is null)
        {
            return new SessionConfig { OnPermissionRequest = PermissionHandler.ApproveAll };
        }

        return new SessionConfig
        {
            SessionId       = config.SessionId,
            ClientName      = config.ClientName,
            Model           = config.Model,
            ReasoningEffort = config.ReasoningEffort,
            SystemMessage   = config.SystemMessage is not null
                ? new SystemMessageConfig { Content = config.SystemMessage }
                : null,
            AvailableTools      = config.AvailableTools?.ToList(),
            ExcludedTools       = config.ExcludedTools?.ToList(),
            OnPermissionRequest = PermissionHandler.ApproveAll
        };
    }
}
