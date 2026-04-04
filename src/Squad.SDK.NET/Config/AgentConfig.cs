namespace Squad.SDK.NET.Config;

public sealed record AgentConfig
{
    public required string Name { get; init; }
    public string? DisplayName { get; init; }
    public required string Role { get; init; }
    public IReadOnlyList<string> Expertise { get; init; } = [];
    public string? Style { get; init; }
    public string? Prompt { get; init; }
    public string? ModelPreference { get; init; }
    public ModelPreference? ModelPreferenceDetail { get; init; }
    public IReadOnlyList<string>? AllowedTools { get; init; }
    public IReadOnlyList<string>? ExcludedTools { get; init; }
    public IReadOnlyList<AgentCapability>? Capabilities { get; init; }
    public BudgetConfig? Budget { get; init; }
    public AgentStatus Status { get; init; } = AgentStatus.Active;
    public string? Charter { get; init; }
}
