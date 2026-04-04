namespace Squad.SDK.NET.Config;

public sealed record ModelSelectionConfig
{
    public string DefaultModel { get; init; } = "gpt-5";
    public ModelTier DefaultTier { get; init; } = ModelTier.Standard;
    public IReadOnlyDictionary<ModelTier, IReadOnlyList<string>> FallbackChains { get; init; } =
        new Dictionary<ModelTier, IReadOnlyList<string>>();
    public bool PreferSameProvider { get; init; }
    public bool RespectTierCeiling { get; init; } = true;
}

public enum ModelTier { Premium, Standard, Fast }
