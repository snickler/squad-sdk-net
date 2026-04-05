namespace Squad.SDK.NET.Tools;

/// <summary>
/// Factory for creating <see cref="SquadToolDefinition"/> instances.
/// </summary>
public static class SquadToolFactory
{
    /// <summary>Defines a squad tool — equivalent to <c>defineTool()</c> in the TypeScript SDK.</summary>
    /// <param name="name">The unique tool name.</param>
    /// <param name="description">Human-readable description of what the tool does.</param>
    /// <param name="parameters">Optional parameter definitions.</param>
    /// <param name="handler">Optional handler invoked when the tool is called.</param>
    /// <param name="agentName">Optional agent name to scope the tool to.</param>
    /// <param name="skipPermission">When <see langword="true"/>, skips permission checks.</param>
    /// <returns>A configured <see cref="SquadToolDefinition"/>.</returns>
    public static SquadToolDefinition Define(
        string name,
        string description,
        IReadOnlyDictionary<string, ToolParameter>? parameters = null,
        Func<IReadOnlyDictionary<string, object?>, Task<SquadToolResult>>? handler = null,
        string? agentName = null,
        bool skipPermission = false)
    {
        return new SquadToolDefinition
        {
            Name = name,
            Description = description,
            Parameters = parameters ?? new Dictionary<string, ToolParameter>(),
            Handler = handler ?? (_ => Task.FromResult(SquadToolResult.Fail("No handler registered."))),
            AgentName = agentName,
            SkipPermission = skipPermission
        };
    }
}
