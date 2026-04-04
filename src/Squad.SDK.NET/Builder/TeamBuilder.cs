using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Builder;

/// <summary>
/// Fluent builder for configuring a <see cref="TeamConfig"/>.
/// </summary>
/// <seealso cref="SquadBuilder"/>
public sealed class TeamBuilder
{
    private string? _name;
    private string? _description;
    private string? _defaultModel;
    private ModelTier _defaultTier = ModelTier.Standard;

    /// <summary>Sets the team name.</summary>
    /// <param name="name">The team name.</param>
    /// <returns>This builder instance for chaining.</returns>
    public TeamBuilder Name(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>Sets the team description.</summary>
    /// <param name="description">A short description of the team's purpose.</param>
    /// <returns>This builder instance for chaining.</returns>
    public TeamBuilder Description(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>Sets the default AI model for all agents in the team.</summary>
    /// <param name="model">The model identifier.</param>
    /// <returns>This builder instance for chaining.</returns>
    public TeamBuilder DefaultModel(string model)
    {
        _defaultModel = model;
        return this;
    }

    /// <summary>Sets the default model tier for the team.</summary>
    /// <param name="tier">The <see cref="ModelTier"/> to use by default.</param>
    /// <returns>This builder instance for chaining.</returns>
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
