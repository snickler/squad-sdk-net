using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Agents;

/// <summary>Compatibility helpers for mapping agent metadata into the current SDK shape.</summary>
public static class AgentMetadataExtensions
{
    /// <summary>Converts a charter into the existing session options shape.</summary>
    public static Squad.SDK.NET.Abstractions.SquadSessionConfig ToSessionConfig(this AgentCharter charter, string? sessionId = null)
        => new()
        {
            SessionId = sessionId,
            ClientName = charter.Name,
            Model = charter.ModelPreference,
            SystemMessage = charter.Prompt,
            AvailableTools = charter.AllowedTools,
            ExcludedTools = charter.ExcludedTools,
        };
}
