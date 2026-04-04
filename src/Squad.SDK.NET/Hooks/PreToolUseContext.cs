namespace Squad.SDK.NET.Hooks;

/// <summary>
/// Provides context to pre-tool-use hooks before a tool is executed.
/// </summary>
/// <seealso cref="PreToolUseResult"/>
public sealed record PreToolUseContext
{
    /// <summary>Gets the name of the tool about to be executed.</summary>
    public required string ToolName { get; init; }

    /// <summary>Gets the arguments that will be passed to the tool.</summary>
    public IReadOnlyDictionary<string, object?> Arguments { get; init; } = new Dictionary<string, object?>();

    /// <summary>Gets the name of the agent requesting the tool call.</summary>
    public required string AgentName { get; init; }

    /// <summary>Gets the session identifier for the current interaction.</summary>
    public required string SessionId { get; init; }
}
