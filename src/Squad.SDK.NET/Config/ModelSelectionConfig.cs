namespace Squad.SDK.NET.Config;

/// <summary>Configuration for model selection, including defaults, fallback chains, and provider preferences.</summary>
public sealed record ModelSelectionConfig
{
    /// <summary>Gets the default model identifier.</summary>
    public string DefaultModel { get; init; } = "gpt-5";

    /// <summary>Gets the default model tier.</summary>
    public ModelTier DefaultTier { get; init; } = ModelTier.Standard;

    /// <summary>Gets the fallback model chains keyed by <see cref="ModelTier"/>.</summary>
    public IReadOnlyDictionary<ModelTier, IReadOnlyList<string>> FallbackChains { get; init; } =
        new Dictionary<ModelTier, IReadOnlyList<string>>();

    /// <summary>Gets a value indicating whether the system prefers models from the same provider.</summary>
    public bool PreferSameProvider { get; init; }

    /// <summary>Gets a value indicating whether the system should not exceed the configured tier ceiling.</summary>
    public bool RespectTierCeiling { get; init; } = true;
}

/// <summary>Represents the quality/cost tier of a model.</summary>
public enum ModelTier
{
    /// <summary>Highest-quality models with premium pricing.</summary>
    Premium,

    /// <summary>Balanced quality and cost models.</summary>
    Standard,

    /// <summary>Low-latency, lower-cost models.</summary>
    Fast
}
