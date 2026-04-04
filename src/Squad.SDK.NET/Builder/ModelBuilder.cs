using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Builder;

public sealed class ModelBuilder
{
    private string _defaultModel = "gpt-5";
    private ModelTier _defaultTier = ModelTier.Standard;
    private readonly Dictionary<ModelTier, IReadOnlyList<string>> _fallbackChains = [];
    private bool _preferSameProvider;

    public ModelBuilder Default(string model)
    {
        _defaultModel = model;
        return this;
    }

    public ModelBuilder DefaultTier(ModelTier tier)
    {
        _defaultTier = tier;
        return this;
    }

    public ModelBuilder FallbackChain(ModelTier tier, params string[] models)
    {
        _fallbackChains[tier] = models.ToList().AsReadOnly();
        return this;
    }

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
