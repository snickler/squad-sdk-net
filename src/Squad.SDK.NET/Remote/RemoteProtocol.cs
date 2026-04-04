using System.Text.Json.Serialization;

namespace Squad.SDK.NET.Remote;

[JsonSerializable(typeof(List<RCAgent>))]
internal sealed partial class RemoteJsonContext : JsonSerializerContext;

/// <summary>
/// Represents a message in the remote protocol.
/// </summary>
public sealed record RCMessage
{
    /// <summary>Gets the message type.</summary>
    public required string Type { get; init; }
    /// <summary>Gets the unique message identifier.</summary>
    public required string Id { get; init; }
    /// <summary>Gets the optional associated session identifier.</summary>
    public string? SessionId { get; init; }
    /// <summary>Gets the optional agent name.</summary>
    public string? AgentName { get; init; }
    /// <summary>Gets the optional message content.</summary>
    public string? Content { get; init; }
    /// <summary>Gets optional metadata key-value pairs.</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    /// <summary>Gets the message timestamp.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents an agent in the remote protocol.
/// </summary>
public sealed record RCAgent
{
    /// <summary>Gets the agent name.</summary>
    public required string Name { get; init; }
    /// <summary>Gets the optional agent role.</summary>
    public string? Role { get; init; }
    /// <summary>Gets the optional agent status.</summary>
    public string? Status { get; init; }
    /// <summary>Gets the optional session identifier the agent belongs to.</summary>
    public string? SessionId { get; init; }
}

/// <summary>
/// Represents a server-to-client event in the remote protocol.
/// </summary>
public sealed record RCServerEvent
{
    /// <summary>Gets the event type identifier.</summary>
    public required string Event { get; init; }
    /// <summary>Gets the optional session identifier.</summary>
    public string? SessionId { get; init; }
    /// <summary>Gets the optional agent name.</summary>
    public string? AgentName { get; init; }
    /// <summary>Gets the optional JSON payload.</summary>
    public System.Text.Json.JsonElement? Data { get; init; }
    /// <summary>Gets the event timestamp.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents a client-to-server command in the remote protocol.
/// </summary>
public sealed record RCClientCommand
{
    /// <summary>Gets the command name.</summary>
    public required string Command { get; init; }
    /// <summary>Gets the optional target agent name.</summary>
    public string? TargetAgent { get; init; }
    /// <summary>Gets optional command parameters.</summary>
    public IReadOnlyDictionary<string, string>? Parameters { get; init; }
}

/// <summary>Well-known remote command names.</summary>
public static class RemoteCommands
{
    /// <summary>Ping command for health checks.</summary>
    public const string Ping = "ping";
    /// <summary>Command to list active agents.</summary>
    public const string ListAgents = "list-agents";
    /// <summary>Command to send a message to an agent.</summary>
    public const string SendMessage = "send-message";
    /// <summary>Command to query bridge status.</summary>
    public const string GetStatus = "get-status";
    /// <summary>Command to shut down the bridge.</summary>
    public const string Shutdown = "shutdown";
}

/// <summary>Well-known remote event names.</summary>
public static class RemoteEvents
{
    /// <summary>Emitted when a client connects.</summary>
    public const string Connected = "connected";
    /// <summary>Emitted when a client disconnects.</summary>
    public const string Disconnected = "disconnected";
    /// <summary>Emitted when a message is received.</summary>
    public const string MessageReceived = "message-received";
    /// <summary>Emitted when an agent is spawned.</summary>
    public const string AgentSpawned = "agent-spawned";
    /// <summary>Emitted when an agent is destroyed.</summary>
    public const string AgentDestroyed = "agent-destroyed";
    /// <summary>Emitted on error.</summary>
    public const string Error = "error";
    /// <summary>Emitted in response to a ping.</summary>
    public const string Pong = "pong";
    /// <summary>Emitted with the list of agents.</summary>
    public const string AgentsListed = "agents-listed";
    /// <summary>Emitted with bridge status information.</summary>
    public const string Status = "status";
}
