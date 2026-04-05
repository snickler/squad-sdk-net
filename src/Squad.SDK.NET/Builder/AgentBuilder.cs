using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Builder;

/// <summary>
/// Fluent builder for configuring an <see cref="AgentConfig"/>.
/// </summary>
/// <seealso cref="SquadBuilder"/>
public sealed class AgentBuilder
{
    private string? _name;
    private string? _displayName;
    private string? _role;
    private readonly List<string> _expertise = [];
    private string? _style;
    private string? _prompt;
    private string? _model;
    private readonly List<string> _allowTools = [];
    private readonly List<string> _excludeTools = [];
    private readonly List<AgentCapability> _capabilities = [];
    private BudgetConfig? _budget;
    private AgentStatus _status = AgentStatus.Active;
    private string? _charter;

    /// <summary>Sets the unique name for this agent.</summary>
    /// <param name="name">The agent name.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentBuilder Name(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>Sets a human-friendly display name for this agent.</summary>
    /// <param name="displayName">The display name.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentBuilder DisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    /// <summary>Sets the role description for this agent.</summary>
    /// <param name="role">A short description of the agent's role.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentBuilder Role(string role)
    {
        _role = role;
        return this;
    }

    /// <summary>Adds expertise areas to the agent.</summary>
    /// <param name="areas">One or more expertise area strings.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentBuilder Expertise(params string[] areas)
    {
        _expertise.AddRange(areas);
        return this;
    }

    /// <summary>Sets the communication style for this agent.</summary>
    /// <param name="style">A style descriptor (e.g., <c>"concise"</c>, <c>"verbose"</c>).</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentBuilder Style(string style)
    {
        _style = style;
        return this;
    }

    /// <summary>Sets a custom system prompt for this agent.</summary>
    /// <param name="prompt">The system prompt text.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentBuilder Prompt(string prompt)
    {
        _prompt = prompt;
        return this;
    }

    /// <summary>Sets the preferred AI model for this agent.</summary>
    /// <param name="model">The model identifier.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentBuilder Model(string model)
    {
        _model = model;
        return this;
    }

    /// <summary>Adds tools to the agent's allowlist.</summary>
    /// <param name="tools">Tool names the agent is allowed to use.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentBuilder AllowTools(params string[] tools)
    {
        _allowTools.AddRange(tools);
        return this;
    }

    /// <summary>Adds tools to the agent's exclusion list.</summary>
    /// <param name="tools">Tool names the agent must not use.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentBuilder ExcludeTools(params string[] tools)
    {
        _excludeTools.AddRange(tools);
        return this;
    }

    /// <summary>Adds capabilities to the agent.</summary>
    /// <param name="capabilities">One or more <see cref="AgentCapability"/> values.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentBuilder Capabilities(params AgentCapability[] capabilities) { _capabilities.AddRange(capabilities); return this; }

    /// <summary>Configures the agent's budget using a builder delegate.</summary>
    /// <param name="configure">A delegate that configures the <see cref="BudgetBuilder"/>.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentBuilder Budget(Action<BudgetBuilder> configure)
    {
        var builder = new BudgetBuilder();
        configure(builder);
        _budget = builder.Build();
        return this;
    }
    /// <summary>Sets the agent's budget from an existing <see cref="BudgetConfig"/>.</summary>
    /// <param name="budget">The budget configuration.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentBuilder Budget(BudgetConfig budget) { _budget = budget; return this; }

    /// <summary>Sets the agent's activation status.</summary>
    /// <param name="status">The <see cref="AgentStatus"/> value.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentBuilder Status(AgentStatus status) { _status = status; return this; }

    /// <summary>Sets a charter string that further defines the agent's mission.</summary>
    /// <param name="charter">The charter text.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentBuilder Charter(string charter) { _charter = charter; return this; }

    internal AgentConfig Build()
    {
        if (string.IsNullOrWhiteSpace(_name))
            throw new InvalidOperationException("Agent name is required.");
        if (string.IsNullOrWhiteSpace(_role))
            throw new InvalidOperationException($"Agent '{_name}' must have a role.");

        return new AgentConfig
        {
            Name = _name,
            DisplayName = _displayName,
            Role = _role,
            Expertise = _expertise.AsReadOnly(),
            Style = _style,
            Prompt = _prompt,
            ModelPreference = _model,
            AllowedTools = _allowTools.Count > 0 ? _allowTools.AsReadOnly() : null,
            ExcludedTools = _excludeTools.Count > 0 ? _excludeTools.AsReadOnly() : null,
            Capabilities = _capabilities.Count > 0 ? _capabilities.AsReadOnly() : null,
            Budget = _budget,
            Status = _status,
            Charter = _charter
        };
    }
}
