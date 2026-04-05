using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Builder;

/// <summary>
/// Fluent builder for configuring a <see cref="CastingConfig"/>.
/// </summary>
/// <seealso cref="SquadBuilder"/>
public sealed class CastingBuilder
{
    private readonly List<string> _allowlistUniverses = [];
    private OverflowStrategy _overflowStrategy = Config.OverflowStrategy.Rotate;
    private int? _capacity;

    /// <summary>Adds universe names to the casting allowlist.</summary>
    /// <param name="universes">Allowed universe identifiers.</param>
    /// <returns>This builder instance for chaining.</returns>
    public CastingBuilder AllowlistUniverses(params string[] universes) { _allowlistUniverses.AddRange(universes); return this; }

    /// <summary>Sets the strategy used when the agent pool is at capacity.</summary>
    /// <param name="strategy">The <see cref="Config.OverflowStrategy"/> to use.</param>
    /// <returns>This builder instance for chaining.</returns>
    public CastingBuilder OverflowStrategy(OverflowStrategy strategy) { _overflowStrategy = strategy; return this; }

    /// <summary>Sets the maximum number of concurrent agents.</summary>
    /// <param name="capacity">The agent capacity limit.</param>
    /// <returns>This builder instance for chaining.</returns>
    public CastingBuilder Capacity(int capacity) { _capacity = capacity; return this; }

    internal CastingConfig Build() => new()
    {
        AllowlistUniverses = new List<string>(_allowlistUniverses).AsReadOnly(),
        OverflowStrategy = _overflowStrategy,
        Capacity = _capacity
    };
}
