using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Abstractions;

namespace Squad.SDK.NET.Remote;

public sealed class RemoteBridge
{
    private readonly ISquadClient _client;
    private readonly ILogger<RemoteBridge> _logger;
    private bool _isRunning;

    public RemoteBridge(ISquadClient client, ILogger<RemoteBridge> logger)
    {
        _client = client;
        _logger = logger;
    }

    public bool IsRunning => _isRunning;

    public async Task<RCServerEvent> HandleCommandAsync(RCClientCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling remote command: {Command}", command.Command);

        return command.Command switch
        {
            RemoteCommands.Ping => new RCServerEvent
            {
                Event = "pong",
                Data = new { status = "ok" }
            },
            RemoteCommands.ListAgents => await HandleListAgentsAsync(cancellationToken),
            RemoteCommands.GetStatus => await HandleGetStatusAsync(cancellationToken),
            _ => new RCServerEvent
            {
                Event = RemoteEvents.Error,
                Data = new { message = $"Unknown command: {command.Command}" }
            }
        };
    }

    public void Start()
    {
        _isRunning = true;
        _logger.LogInformation("Remote bridge started");
    }

    public void Stop()
    {
        _isRunning = false;
        _logger.LogInformation("Remote bridge stopped");
    }

    private async Task<RCServerEvent> HandleListAgentsAsync(CancellationToken cancellationToken)
    {
        var sessions = await _client.ListSessionsAsync(cancellationToken);
        var agents = sessions.Select(s => new RCAgent
        {
            Name = s.AgentName ?? "unknown",
            Status = "active",
            SessionId = s.SessionId
        }).ToList();

        return new RCServerEvent
        {
            Event = "agents-listed",
            Data = agents
        };
    }

    private async Task<RCServerEvent> HandleGetStatusAsync(CancellationToken cancellationToken)
    {
        var sessions = await _client.ListSessionsAsync(cancellationToken);
        return new RCServerEvent
        {
            Event = "status",
            Data = new { isRunning = _isRunning, activeSessions = sessions.Count }
        };
    }
}
