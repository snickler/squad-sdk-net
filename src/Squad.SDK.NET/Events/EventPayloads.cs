namespace Squad.SDK.NET.Events;

/// <summary>
/// Payload for a streaming message delta event.
/// </summary>
public sealed record StreamDeltaPayload
{
    /// <summary>Gets the incremental content fragment.</summary>
    public required string Content { get; init; }

    /// <summary>Gets the zero-based index of this delta within the stream.</summary>
    public int Index { get; init; }
}

/// <summary>
/// Payload containing token usage statistics for a model interaction.
/// </summary>
public sealed record UsagePayload
{
    /// <summary>Gets the model identifier that produced the usage.</summary>
    public required string Model { get; init; }

    /// <summary>Gets the number of input tokens consumed.</summary>
    public int InputTokens { get; init; }

    /// <summary>Gets the number of output tokens produced.</summary>
    public int OutputTokens { get; init; }

    /// <summary>Gets the estimated monetary cost of the interaction.</summary>
    public decimal EstimatedCost { get; init; }
}

/// <summary>
/// Payload for a streaming reasoning delta event.
/// </summary>
public sealed record ReasoningDeltaPayload
{
    /// <summary>Gets the incremental reasoning content fragment.</summary>
    public required string Content { get; init; }

    /// <summary>Gets the zero-based index of this delta within the reasoning stream.</summary>
    public int Index { get; init; }
}

/// <summary>
/// Payload describing an error that occurred within a session.
/// </summary>
public sealed record SessionErrorPayload
{
    /// <summary>Gets the human-readable error message.</summary>
    public required string Message { get; init; }

    /// <summary>Gets the underlying exception, if available.</summary>
    public Exception? Exception { get; init; }
}

/// <summary>
/// Payload describing a tool call initiated or completed by an agent.
/// </summary>
/// <seealso cref="ToolCallStatus"/>
public sealed record ToolCallPayload
{
    /// <summary>Gets the name of the tool that was called.</summary>
    public required string ToolName { get; init; }

    /// <summary>Gets the arguments passed to the tool, if available.</summary>
    public IReadOnlyDictionary<string, object?>? Arguments { get; init; }

    /// <summary>Gets the current status of the tool call.</summary>
    public ToolCallStatus Status { get; init; }
}

/// <summary>
/// Represents the execution status of a tool call.
/// </summary>
public enum ToolCallStatus
{
    /// <summary>The tool call is currently executing.</summary>
    Running,

    /// <summary>The tool call completed successfully.</summary>
    Completed,

    /// <summary>The tool call failed with an error.</summary>
    Error
}

/// <summary>
/// Payload emitted when a sub-agent is spawned by a parent agent.
/// </summary>
public sealed record SubAgentSpawnedPayload
{
    /// <summary>Gets the name of the parent agent that spawned the sub-agent.</summary>
    public required string ParentAgentName { get; init; }

    /// <summary>Gets the name of the newly spawned child agent.</summary>
    public required string ChildAgentName { get; init; }

    /// <summary>Gets the nesting depth of this sub-agent in the agent hierarchy.</summary>
    public int Depth { get; init; }
}
