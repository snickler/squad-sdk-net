using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Builder;

/// <summary>
/// Fluent builder for configuring a <see cref="ModelSelectionConfig"/>.
/// </summary>
/// <seealso cref="SquadBuilder"/>
public sealed class ModelBuilder
{
    private string _defaultModel = "gpt-5";
    private ModelTier _defaultTier = ModelTier.Standard;
    private readonly Dictionary<ModelTier, IReadOnlyList<string>> _fallbackChains = [];
    private bool _preferSameProvider;

    /// <summary>Sets the default model identifier.</summary>
    /// <param name="model">The model identifier.</param>
    /// <returns>This builder instance for chaining.</returns>
    public ModelBuilder Default(string model)
    {
        _defaultModel = model;
        return this;
    }

    /// <summary>Sets the default model tier.</summary>
    /// <param name="tier">The <see cref="ModelTier"/> to use by default.</param>
    /// <returns>This builder instance for chaining.</returns>
    public ModelBuilder DefaultTier(ModelTier tier)
    {
        _defaultTier = tier;
        return this;
    }

    /// <summary>Defines a fallback chain of models for a specific tier.</summary>
    /// <param name="tier">The <see cref="ModelTier"/> this chain applies to.</param>
    /// <param name="models">Ordered list of model identifiers to try.</param>
    /// <returns>This builder instance for chaining.</returns>
    public ModelBuilder FallbackChain(ModelTier tier, params string[] models)
    {
        _fallbackChains[tier] = models.ToList().AsReadOnly();
        return this;
    }

    /// <summary>Sets whether the selector should prefer models from the same provider.</summary>
    /// <param name="value"><see langword="true"/> to prefer same-provider models.</param>
    /// <returns>This builder instance for chaining.</returns>
    public ModelBuilder PreferSameProvider(bool value = true)
    {
        _preferSameProvider = value;
        return this;
    }

    internal ModelSelectionConfig Build() => new()
    {
        DefaultModel = _defaultModel,
        DefaultTier = _defaultTier,
        FallbackChains = _fallbackChains,
        PreferSameProvider = _preferSameProvider
    };
}
