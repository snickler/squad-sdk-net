namespace Squad.SDK.NET.Abstractions;

/// <summary>
/// Configuration for creating or resuming an <see cref="ISquadSession"/>.
/// </summary>
public sealed record SquadSessionConfig
{
    /// <summary>An optional session identifier; one is generated when <see langword="null"/>.</summary>
    public string? SessionId { get; init; }

    /// <summary>Display name for the client within this session.</summary>
    public string? ClientName { get; init; }

    /// <summary>The AI model identifier to use (e.g., <c>"claude-sonnet-4.5"</c>).</summary>
    public string? Model { get; init; }

    /// <summary>Reasoning effort level: "low", "medium", "high", or "xhigh".</summary>
    public string? ReasoningEffort { get; init; }

    /// <summary>A system message prepended to every conversation in this session.</summary>
    public string? SystemMessage { get; init; }

    /// <summary>An allowlist of tool names the agent may use; <see langword="null"/> means all tools are available.</summary>
    public IReadOnlyList<string>? AvailableTools { get; init; }

    /// <summary>A list of tool names explicitly excluded from use in this session.</summary>
    public IReadOnlyList<string>? ExcludedTools { get; init; }
}
