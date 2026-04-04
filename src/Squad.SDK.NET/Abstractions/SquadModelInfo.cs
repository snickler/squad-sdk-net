namespace Squad.SDK.NET.Abstractions;

/// <summary>
/// Information about an available AI model.
/// </summary>
public sealed record SquadModelInfo
{
    /// <summary>
    /// Model identifier (e.g., "claude-sonnet-4.5", "gpt-5-mini").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-friendly display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Supported reasoning effort levels (e.g., "low", "medium", "high").
    /// Null or empty if the model does not support reasoning effort.
    /// </summary>
    public IReadOnlyList<string>? SupportedReasoningEfforts { get; init; }

    /// <summary>
    /// Default reasoning effort level, if the model supports it.
    /// </summary>
    public string? DefaultReasoningEffort { get; init; }

    /// <summary>
    /// Whether this model supports reasoning effort configuration.
    /// </summary>
    public bool SupportsReasoningEffort =>
        SupportedReasoningEfforts is { Count: > 0 };
}
