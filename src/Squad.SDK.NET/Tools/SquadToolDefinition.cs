namespace Squad.SDK.NET.Tools;

public sealed record SquadToolDefinition
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public IReadOnlyDictionary<string, ToolParameter> Parameters { get; init; } = new Dictionary<string, ToolParameter>();
    public required Func<IReadOnlyDictionary<string, object?>, Task<SquadToolResult>> Handler { get; init; }
    public string? AgentName { get; init; }
    public bool SkipPermission { get; init; }
}

public sealed record ToolParameter
{
    public required string Type { get; init; }
    public string? Description { get; init; }
    public bool Required { get; init; }
}

public sealed record SquadToolResult
{
    public bool Success { get; init; }
    public required string Message { get; init; }
    public object? Data { get; init; }

    public static SquadToolResult Ok(string message, object? data = null) =>
        new() { Success = true, Message = message, Data = data };

    public static SquadToolResult Fail(string message) =>
        new() { Success = false, Message = message };
}
