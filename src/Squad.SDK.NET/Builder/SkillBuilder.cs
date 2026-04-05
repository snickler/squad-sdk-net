using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Builder;

/// <summary>
/// Fluent builder for configuring a <see cref="SkillConfig"/>.
/// </summary>
/// <seealso cref="SquadBuilder"/>
public sealed class SkillBuilder
{
    private string? _name;
    private string? _description;
    private string? _domain;
    private SkillConfidenceLevel _confidence = SkillConfidenceLevel.Medium;
    private string? _source;
    private string? _content;
    private readonly List<string> _tools = [];

    /// <summary>Sets the skill name.</summary>
    /// <param name="name">The skill name.</param>
    /// <returns>This builder instance for chaining.</returns>
    public SkillBuilder Name(string name) { _name = name; return this; }

    /// <summary>Sets a description of what the skill does.</summary>
    /// <param name="description">The skill description.</param>
    /// <returns>This builder instance for chaining.</returns>
    public SkillBuilder Description(string description) { _description = description; return this; }

    /// <summary>Sets the domain this skill belongs to.</summary>
    /// <param name="domain">The domain name.</param>
    /// <returns>This builder instance for chaining.</returns>
    public SkillBuilder Domain(string domain) { _domain = domain; return this; }

    /// <summary>Sets the confidence level for this skill.</summary>
    /// <param name="confidence">The <see cref="SkillConfidenceLevel"/>.</param>
    /// <returns>This builder instance for chaining.</returns>
    public SkillBuilder Confidence(SkillConfidenceLevel confidence) { _confidence = confidence; return this; }

    /// <summary>Sets the source reference for this skill's knowledge.</summary>
    /// <param name="source">A URI or path to the skill source.</param>
    /// <returns>This builder instance for chaining.</returns>
    public SkillBuilder Source(string source) { _source = source; return this; }

    /// <summary>Sets the inline content for this skill.</summary>
    /// <param name="content">The skill content text.</param>
    /// <returns>This builder instance for chaining.</returns>
    public SkillBuilder Content(string content) { _content = content; return this; }

    /// <summary>Adds tool names associated with this skill.</summary>
    /// <param name="tools">Tool names this skill provides.</param>
    /// <returns>This builder instance for chaining.</returns>
    public SkillBuilder Tools(params string[] tools) { _tools.AddRange(tools); return this; }

    internal SkillConfig Build()
    {
        if (string.IsNullOrWhiteSpace(_name))
            throw new InvalidOperationException("Skill name is required.");

        return new SkillConfig
        {
            Name = _name,
            Description = _description,
            Domain = _domain,
            Confidence = _confidence,
            Source = _source,
            Content = _content,
            Tools = _tools.AsReadOnly()
        };
    }
}
