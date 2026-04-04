namespace Squad.SDK.NET.Config;

public sealed record BudgetConfig
{
    public decimal? PerAgentSpawn { get; init; }
    public decimal? PerSession { get; init; }
    public decimal? WarnAt { get; init; }
}
