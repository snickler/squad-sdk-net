namespace Squad.SDK.NET.Tools;

/// <summary>
/// Built-in squad tools that mirror the TypeScript SDK's built-in tool set.
/// </summary>
public static class BuiltInTools
{
    /// <summary>squad_route — Route a task to a specific agent.</summary>
    public static SquadToolDefinition SquadRoute(Func<string, string, Task<SquadToolResult>> handler) =>
        SquadToolFactory.Define(
            name: "squad_route",
            description: "Route a task to a specific agent",
            parameters: new Dictionary<string, ToolParameter>
            {
                ["task"]  = new() { Type = "string", Description = "The task description to route", Required = true },
                ["agent"] = new() { Type = "string", Description = "The name of the target agent",  Required = true }
            },
            handler: async args =>
            {
                var task  = GetString(args, "task");
                var agent = GetString(args, "agent");
                return await handler(task, agent).ConfigureAwait(false);
            },
            skipPermission: true);

    /// <summary>squad_decide — Record an architectural decision.</summary>
    public static SquadToolDefinition SquadDecide(Func<string, string, Task<SquadToolResult>> handler) =>
        SquadToolFactory.Define(
            name: "squad_decide",
            description: "Record an architectural or design decision",
            parameters: new Dictionary<string, ToolParameter>
            {
                ["decision"]  = new() { Type = "string", Description = "The decision being made",    Required = true },
                ["rationale"] = new() { Type = "string", Description = "Reasoning behind the decision", Required = true }
            },
            handler: async args =>
            {
                var decision  = GetString(args, "decision");
                var rationale = GetString(args, "rationale");
                return await handler(decision, rationale).ConfigureAwait(false);
            },
            skipPermission: true);

    /// <summary>squad_memory — Store or retrieve agent memory.</summary>
    public static SquadToolDefinition SquadMemory(Func<string, string, string, Task<SquadToolResult>> handler) =>
        SquadToolFactory.Define(
            name: "squad_memory",
            description: "Store or retrieve agent memory by key",
            parameters: new Dictionary<string, ToolParameter>
            {
                ["operation"] = new() { Type = "string", Description = "The operation: 'get' or 'set'", Required = true },
                ["key"]       = new() { Type = "string", Description = "The memory key",               Required = true },
                ["value"]     = new() { Type = "string", Description = "The value to store (for 'set' operations)", Required = false }
            },
            handler: async args =>
            {
                var operation = GetString(args, "operation");
                var key       = GetString(args, "key");
                var value     = GetString(args, "value");
                return await handler(operation, key, value).ConfigureAwait(false);
            },
            skipPermission: true);

    /// <summary>squad_status — Query the status of an agent or all agents.</summary>
    public static SquadToolDefinition SquadStatus(Func<string?, Task<SquadToolResult>> handler) =>
        SquadToolFactory.Define(
            name: "squad_status",
            description: "Query the current status of one or all agents",
            parameters: new Dictionary<string, ToolParameter>
            {
                ["agentName"] = new() { Type = "string", Description = "Agent name to query, or omit for all agents", Required = false }
            },
            handler: async args =>
            {
                var agentName = args.TryGetValue("agentName", out var v) ? v?.ToString() : null;
                return await handler(agentName).ConfigureAwait(false);
            },
            skipPermission: true);

    private static string GetString(IReadOnlyDictionary<string, object?> args, string key) =>
        args.TryGetValue(key, out var v) ? v?.ToString() ?? string.Empty : string.Empty;
}
