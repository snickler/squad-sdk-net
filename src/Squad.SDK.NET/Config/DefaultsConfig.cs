namespace Squad.SDK.NET.Config;

public sealed record DefaultsConfig
{
    public string? Model { get; init; }
    public ModelPreference? ModelPreference { get; init; }
    public BudgetConfig? Budget { get; init; }
}
