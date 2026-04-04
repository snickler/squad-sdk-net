namespace Squad.SDK.NET.Abstractions;

public sealed record SquadSessionConfig
{
    public string? SessionId { get; init; }
    public string? ClientName { get; init; }
    public string? Model { get; init; }
    /// <summary>Reasoning effort level: "low", "medium", "high", or "xhigh".</summary>
    public string? ReasoningEffort { get; init; }
    public string? SystemMessage { get; init; }
    public IReadOnlyList<string>? AvailableTools { get; init; }
    public IReadOnlyList<string>? ExcludedTools { get; init; }
}
