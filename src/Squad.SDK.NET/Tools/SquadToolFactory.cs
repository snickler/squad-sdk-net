namespace Squad.SDK.NET.Tools;

public static class SquadToolFactory
{
    /// <summary>Defines a squad tool — equivalent to <c>defineTool()</c> in the TypeScript SDK.</summary>
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
