namespace Squad.SDK.NET.Hooks;

public sealed record PreToolUseContext
{
    public required string ToolName { get; init; }
    public IReadOnlyDictionary<string, object?> Arguments { get; init; } = new Dictionary<string, object?>();
    public required string AgentName { get; init; }
    public required string SessionId { get; init; }
}
