namespace Squad.SDK.NET.Hooks;

/// <summary>
/// Provides context to post-tool-use hooks after a tool has executed.
/// </summary>
/// <seealso cref="PostToolUseResult"/>
public sealed record PostToolUseContext
{
    /// <summary>Gets the name of the tool that was executed.</summary>
    public required string ToolName { get; init; }

    /// <summary>Gets the arguments that were passed to the tool.</summary>
    public IReadOnlyDictionary<string, object?> Arguments { get; init; } = new Dictionary<string, object?>();

    /// <summary>Gets the result returned by the tool, if any.</summary>
    public object? Result { get; init; }

    /// <summary>Gets the name of the agent that executed the tool.</summary>
    public required string AgentName { get; init; }

    /// <summary>Gets the session identifier for the current interaction.</summary>
    public required string SessionId { get; init; }
}
