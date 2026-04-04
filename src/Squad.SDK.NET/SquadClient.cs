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

    /// <summary>
    /// Initializes a new instance of the <see cref="SquadClient"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory used to create loggers for the client and its sessions.</param>
    /// <param name="options">Optional configuration for the underlying <see cref="CopilotClient"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="loggerFactory"/> is <see langword="null"/>.</exception>
    public SquadClient(ILoggerFactory loggerFactory, CopilotClientOptions? options = null)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = _loggerFactory.CreateLogger<SquadClient>();
        _copilotClient = new CopilotClient(options ?? new CopilotClientOptions());
    }

    /// <inheritdoc />
    public Abstractions.ConnectionState State
    {
        get
        {
            var state = MapConnectionState(_copilotClient.State);
            _logger.LogDebug("Client state: {State}", state);
            return state;
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        => _copilotClient.DeleteSessionAsync(sessionId, cancellationToken);

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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
