namespace Squad.SDK.NET.Agents;

public sealed record AgentCharter
{
    public required string Name { get; init; }
    public string? DisplayName { get; init; }
    public required string Role { get; init; }
    public IReadOnlyList<string> Expertise { get; init; } = [];
    public string? Style { get; init; }
    public required string Prompt { get; init; }
    public IReadOnlyList<string>? AllowedTools { get; init; }
    public IReadOnlyList<string>? ExcludedTools { get; init; }
    public string? ModelPreference { get; init; }
}
