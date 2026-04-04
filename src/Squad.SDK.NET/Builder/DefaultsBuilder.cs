using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Builder;

public sealed class DefaultsBuilder
{
    private string? _model;
    private ModelPreference? _modelPreference;
    private BudgetConfig? _budget;

    public DefaultsBuilder Model(string model) { _model = model; return this; }
    public DefaultsBuilder Model(ModelPreference preference) { _modelPreference = preference; _model = preference.Preferred; return this; }
    public DefaultsBuilder Budget(BudgetConfig budget) { _budget = budget; return this; }
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
