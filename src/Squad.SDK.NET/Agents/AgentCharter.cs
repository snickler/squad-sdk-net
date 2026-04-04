namespace Squad.SDK.NET.Agents;

/// <summary>Defines an agent's charter, including its identity, role, expertise, and prompting configuration.</summary>
/// <seealso cref="CharterCompiler"/>
public sealed record AgentCharter
{
    /// <summary>Gets the unique name of the agent.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the display-friendly name of the agent.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Gets the role the agent fulfils within the squad.</summary>
    public required string Role { get; init; }

    /// <summary>Gets the list of expertise areas for the agent.</summary>
    public IReadOnlyList<string> Expertise { get; init; } = [];

    /// <summary>Gets the communication style hint for the agent.</summary>
    public string? Style { get; init; }

    /// <summary>Gets the system prompt used to initialize the agent session.</summary>
    public required string Prompt { get; init; }

    /// <summary>Gets the explicit list of tools the agent is allowed to use, or <see langword="null"/> if unrestricted.</summary>
    public IReadOnlyList<string>? AllowedTools { get; init; }

    /// <summary>Gets the list of tools the agent is forbidden from using.</summary>
    public IReadOnlyList<string>? ExcludedTools { get; init; }

    /// <summary>Gets the preferred model identifier for this agent.</summary>
    public string? ModelPreference { get; init; }
}
