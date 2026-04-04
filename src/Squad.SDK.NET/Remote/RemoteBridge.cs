using System.Text.Json;
using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Abstractions;

namespace Squad.SDK.NET.Remote;

/// <summary>
/// Bridges remote commands to the local <see cref="ISquadClient"/>, handling RPC-style requests.
/// </summary>
public sealed class RemoteBridge
{
    private readonly ISquadClient _client;
    private readonly ILogger<RemoteBridge> _logger;
    private bool _isRunning;

    /// <summary>
    /// Initializes a new <see cref="RemoteBridge"/>.
    /// </summary>
    /// <param name="client">The squad client to delegate commands to.</param>
    /// <param name="logger">Logger instance.</param>
    public RemoteBridge(ISquadClient client, ILogger<RemoteBridge> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>Gets a value indicating whether the bridge is currently running.</summary>
    public bool IsRunning => _isRunning;

    /// <summary>Handles a remote command and returns the corresponding server event.</summary>
    /// <param name="command">The incoming client command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="RCServerEvent"/> containing the response.</returns>
    public async Task<RCServerEvent> HandleCommandAsync(RCClientCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling remote command: {Command}", command.Command);

        return command.Command switch
        {
            RemoteCommands.Ping => new RCServerEvent
            {
                Event = RemoteEvents.Pong,
                Data = JsonDocument.Parse("""{"status":"ok"}""").RootElement.Clone()
            },
            RemoteCommands.ListAgents => await HandleListAgentsAsync(cancellationToken),
            RemoteCommands.GetStatus => await HandleGetStatusAsync(cancellationToken),
            _ => new RCServerEvent
            {
                Event = RemoteEvents.Error,
                Data = JsonDocument.Parse($$$"""{"message":"Unknown command: {{{command.Command}}}"}""").RootElement.Clone()
            }
        };
    }

    /// <summary>Starts the remote bridge.</summary>
    public void Start()
    {
        _isRunning = true;
        _logger.LogInformation("Remote bridge started");
    }

    /// <summary>Stops the remote bridge.</summary>
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
            Event = RemoteEvents.AgentsListed,
            Data = JsonSerializer.SerializeToElement(agents, RemoteJsonContext.Default.ListRCAgent)
        };
    }

    private async Task<RCServerEvent> HandleGetStatusAsync(CancellationToken cancellationToken)
    {
        var sessions = await _client.ListSessionsAsync(cancellationToken);
        return new RCServerEvent
        {
            Event = RemoteEvents.Status,
            Data = JsonDocument.Parse($$$"""{"isRunning":{{{(_isRunning ? "true" : "false")}}},"activeSessions":{{{sessions.Count}}}}""").RootElement.Clone()
        };
    }
}
