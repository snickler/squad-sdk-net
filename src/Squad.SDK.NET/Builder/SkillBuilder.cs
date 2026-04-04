using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Builder;

public sealed class SkillBuilder
{
    private string? _name;
    private string? _description;
    private string? _domain;
    private SkillConfidenceLevel _confidence = SkillConfidenceLevel.Medium;
    private string? _source;
    private string? _content;
    private readonly List<string> _tools = [];

    public SkillBuilder Name(string name) { _name = name; return this; }
    public SkillBuilder Description(string description) { _description = description; return this; }
    public SkillBuilder Domain(string domain) { _domain = domain; return this; }
    public SkillBuilder Confidence(SkillConfidenceLevel confidence) { _confidence = confidence; return this; }
    public SkillBuilder Source(string source) { _source = source; return this; }
    public SkillBuilder Content(string content) { _content = content; return this; }
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
