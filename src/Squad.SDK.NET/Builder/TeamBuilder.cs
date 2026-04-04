using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Builder;

public sealed class TeamBuilder
{
    private string? _name;
    private string? _description;
    private string? _defaultModel;
    private ModelTier _defaultTier = ModelTier.Standard;

    public TeamBuilder Name(string name)
    {
        _name = name;
        return this;
    }

    public TeamBuilder Description(string description)
    {
        _description = description;
        return this;
    }

    public TeamBuilder DefaultModel(string model)
    {
        _defaultModel = model;
        return this;
    }

    public TeamBuilder DefaultTier(ModelTier tier)
    {
        _defaultTier = tier;
        return this;
    }

    internal TeamConfig Build()
    {
        if (string.IsNullOrWhiteSpace(_name))
            throw new InvalidOperationException("Team name is required.");

        return new TeamConfig
        {
            Name = _name,
            Description = _description,
            DefaultModel = _defaultModel,
            DefaultTier = _defaultTier
        };
    }
}
