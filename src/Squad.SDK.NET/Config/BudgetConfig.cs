namespace Squad.SDK.NET.Config;

/// <summary>Configuration for budget constraints applied to agents or sessions.</summary>
public sealed record BudgetConfig
{
    /// <summary>Gets the maximum budget allowed per agent spawn.</summary>
    public decimal? PerAgentSpawn { get; init; }

    /// <summary>Gets the maximum budget allowed per session.</summary>
    public decimal? PerSession { get; init; }

    /// <summary>Gets the budget threshold at which a warning is emitted.</summary>
    public decimal? WarnAt { get; init; }
}
