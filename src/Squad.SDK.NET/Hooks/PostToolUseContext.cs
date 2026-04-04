namespace Squad.SDK.NET.Hooks;

public sealed record PostToolUseContext
{
    public required string ToolName { get; init; }
    public IReadOnlyDictionary<string, object?> Arguments { get; init; } = new Dictionary<string, object?>();
    public object? Result { get; init; }
    public required string AgentName { get; init; }
    public required string SessionId { get; init; }
}
