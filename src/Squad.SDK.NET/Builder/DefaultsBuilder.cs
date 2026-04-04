using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Builder;

/// <summary>
/// Fluent builder for configuring a <see cref="DefaultsConfig"/>.
/// </summary>
/// <seealso cref="SquadBuilder"/>
public sealed class DefaultsBuilder
{
    private string? _model;
    private ModelPreference? _modelPreference;
    private BudgetConfig? _budget;

    /// <summary>Sets the default model identifier.</summary>
    /// <param name="model">The model identifier.</param>
    /// <returns>This builder instance for chaining.</returns>
    public DefaultsBuilder Model(string model) { _model = model; return this; }

    /// <summary>Sets the default model from a <see cref="ModelPreference"/>.</summary>
    /// <param name="preference">The model preference containing preferred and fallback models.</param>
    /// <returns>This builder instance for chaining.</returns>
    public DefaultsBuilder Model(ModelPreference preference) { _modelPreference = preference; _model = preference.Preferred; return this; }

    /// <summary>Sets the default budget from an existing <see cref="BudgetConfig"/>.</summary>
    /// <param name="budget">The budget configuration.</param>
    /// <returns>This builder instance for chaining.</returns>
    public DefaultsBuilder Budget(BudgetConfig budget) { _budget = budget; return this; }

    /// <summary>Configures the default budget using a builder delegate.</summary>
    /// <param name="configure">A delegate that configures the <see cref="BudgetBuilder"/>.</param>
    /// <returns>This builder instance for chaining.</returns>
    public DefaultsBuilder Budget(Action<BudgetBuilder> configure)
    {
        var builder = new BudgetBuilder();
        configure(builder);
        _budget = builder.Build();
        return this;
    }

    internal DefaultsConfig Build() => new()
    {
        Model = _model,
        ModelPreference = _modelPreference,
        Budget = _budget
    };
}
