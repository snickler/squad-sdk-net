namespace Squad.SDK.NET.Remote;

public sealed record RCMessage
{
    public required string Type { get; init; }
    public required string Id { get; init; }
    public string? SessionId { get; init; }
    public string? AgentName { get; init; }
    public string? Content { get; init; }
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record RCAgent
{
    public required string Name { get; init; }
    public string? Role { get; init; }
    public string? Status { get; init; }
    public string? SessionId { get; init; }
}

public sealed record RCServerEvent
{
    public required string Event { get; init; }
    public string? SessionId { get; init; }
    public string? AgentName { get; init; }
    public object? Data { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record RCClientCommand
{
    public required string Command { get; init; }
    public string? TargetAgent { get; init; }
    public IReadOnlyDictionary<string, string>? Parameters { get; init; }
}

public static class RemoteCommands
{
    public const string Ping = "ping";
    public const string ListAgents = "list-agents";
    public const string SendMessage = "send-message";
    public const string GetStatus = "get-status";
    public const string Shutdown = "shutdown";
}

public static class RemoteEvents
{
    public const string Connected = "connected";
    public const string Disconnected = "disconnected";
    public const string MessageReceived = "message-received";
    public const string AgentSpawned = "agent-spawned";
    public const string AgentDestroyed = "agent-destroyed";
    public const string Error = "error";
}
