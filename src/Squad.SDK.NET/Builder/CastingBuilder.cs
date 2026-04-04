using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Builder;

public sealed class CastingBuilder
{
    private readonly List<string> _allowlistUniverses = [];
    private OverflowStrategy _overflowStrategy = Config.OverflowStrategy.Rotate;
    private int? _capacity;

    public CastingBuilder AllowlistUniverses(params string[] universes) { _allowlistUniverses.AddRange(universes); return this; }
    public CastingBuilder OverflowStrategy(OverflowStrategy strategy) { _overflowStrategy = strategy; return this; }
    public CastingBuilder Capacity(int capacity) { _capacity = capacity; return this; }

    internal CastingConfig Build() => new()
    {
        AllowlistUniverses = _allowlistUniverses.AsReadOnly(),
        OverflowStrategy = _overflowStrategy,
        Capacity = _capacity
    };
}
