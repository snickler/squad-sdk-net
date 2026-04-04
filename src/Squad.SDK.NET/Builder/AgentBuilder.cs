using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Builder;

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

    public AgentBuilder Name(string name)
    {
        _name = name;
        return this;
    }

    public AgentBuilder DisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    public AgentBuilder Role(string role)
    {
        _role = role;
        return this;
    }

    public AgentBuilder Expertise(params string[] areas)
    {
        _expertise.AddRange(areas);
        return this;
    }

    public AgentBuilder Style(string style)
    {
        _style = style;
        return this;
    }

    public AgentBuilder Prompt(string prompt)
    {
        _prompt = prompt;
        return this;
    }

    public AgentBuilder Model(string model)
    {
        _model = model;
        return this;
    }

    public AgentBuilder AllowTools(params string[] tools)
    {
        _allowTools.AddRange(tools);
        return this;
    }

    public AgentBuilder ExcludeTools(params string[] tools)
    {
        _excludeTools.AddRange(tools);
        return this;
    }

    public AgentBuilder Capabilities(params AgentCapability[] capabilities) { _capabilities.AddRange(capabilities); return this; }
    public AgentBuilder Budget(Action<BudgetBuilder> configure)
    {
        var builder = new BudgetBuilder();
        configure(builder);
        _budget = builder.Build();
        return this;
    }
    public AgentBuilder Budget(BudgetConfig budget) { _budget = budget; return this; }
    public AgentBuilder Status(AgentStatus status) { _status = status; return this; }
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
