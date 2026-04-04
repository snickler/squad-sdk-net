namespace Squad.SDK.NET.Events;

public sealed record StreamDeltaPayload
{
    public required string Content { get; init; }
    public int Index { get; init; }
}

public sealed record UsagePayload
{
    public required string Model { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public decimal EstimatedCost { get; init; }
}

public sealed record ReasoningDeltaPayload
{
    public required string Content { get; init; }
    public int Index { get; init; }
}

public sealed record SessionErrorPayload
{
    public required string Message { get; init; }
    public Exception? Exception { get; init; }
}

public sealed record ToolCallPayload
{
    public required string ToolName { get; init; }
    public IReadOnlyDictionary<string, object?>? Arguments { get; init; }
    public ToolCallStatus Status { get; init; }
}

public enum ToolCallStatus { Running, Completed, Error }

public sealed record SubAgentSpawnedPayload
{
    public required string ParentAgentName { get; init; }
    public required string ChildAgentName { get; init; }
    public int Depth { get; init; }
}
