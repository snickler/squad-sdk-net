namespace Squad.SDK.NET.Tools;

/// <summary>
/// Defines a tool that agents can invoke during squad execution.
/// </summary>
public sealed record SquadToolDefinition
{
    /// <summary>Gets the tool name (must be unique within a squad).</summary>
    public required string Name { get; init; }
    /// <summary>Gets the human-readable tool description.</summary>
    public required string Description { get; init; }
    /// <summary>Gets the parameter definitions keyed by parameter name.</summary>
    public IReadOnlyDictionary<string, ToolParameter> Parameters { get; init; } = new Dictionary<string, ToolParameter>();
    /// <summary>Gets the asynchronous handler invoked when the tool is called.</summary>
    public required Func<IReadOnlyDictionary<string, object?>, Task<SquadToolResult>> Handler { get; init; }
    /// <summary>Gets the optional agent name this tool is scoped to.</summary>
    public string? AgentName { get; init; }
    /// <summary>Gets a value indicating whether to skip user-permission checks.</summary>
    public bool SkipPermission { get; init; }
}

/// <summary>
/// Describes a single tool parameter.
/// </summary>
public sealed record ToolParameter
{
    /// <summary>Gets the parameter type (e.g., "string", "number").</summary>
    public required string Type { get; init; }
    /// <summary>Gets an optional description of the parameter.</summary>
    public string? Description { get; init; }
    /// <summary>Gets a value indicating whether this parameter is required.</summary>
    public bool Required { get; init; }
}

/// <summary>
/// Represents the result of a tool invocation.
/// </summary>
public sealed record SquadToolResult
{
    /// <summary>Gets a value indicating whether the tool call succeeded.</summary>
    public bool Success { get; init; }
    /// <summary>Gets the result message.</summary>
    public required string Message { get; init; }
    /// <summary>Gets optional result data.</summary>
    public object? Data { get; init; }

    /// <summary>Creates a successful result.</summary>
    /// <param name="message">Success message.</param>
    /// <param name="data">Optional data payload.</param>
    /// <returns>A successful <see cref="SquadToolResult"/>.</returns>
    public static SquadToolResult Ok(string message, object? data = null) =>
        new() { Success = true, Message = message, Data = data };

    /// <summary>Creates a failure result.</summary>
    /// <param name="message">Error message.</param>
    /// <returns>A failed <see cref="SquadToolResult"/>.</returns>
    public static SquadToolResult Fail(string message) =>
        new() { Success = false, Message = message };
}
