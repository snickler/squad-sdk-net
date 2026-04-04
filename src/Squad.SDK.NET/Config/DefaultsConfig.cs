namespace Squad.SDK.NET.Config;

/// <summary>Default settings applied to all agents unless overridden at the agent level.</summary>
public sealed record DefaultsConfig
{
    /// <summary>Gets the default model identifier.</summary>
    public string? Model { get; init; }

    /// <summary>Gets the default model preference with fallback information.</summary>
    public ModelPreference? ModelPreference { get; init; }

    /// <summary>Gets the default budget constraints.</summary>
    public BudgetConfig? Budget { get; init; }
}
