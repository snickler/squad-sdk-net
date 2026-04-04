using System.Collections.Concurrent;

namespace Squad.SDK.NET.Skills;

public sealed class SkillRegistry
{
    private readonly ConcurrentDictionary<string, SkillDefinition> _skills = new();

    public void Register(SkillDefinition skill) =>
        _skills[skill.Id] = skill;

    public bool Unregister(string skillId) =>
        _skills.TryRemove(skillId, out _);

    public SkillDefinition? Get(string skillId) =>
        _skills.TryGetValue(skillId, out var skill) ? skill : null;

    public IReadOnlyList<SkillDefinition> GetAll() =>
        _skills.Values.ToList().AsReadOnly();

    /// <summary>
    /// Matches skills by keyword overlap between task description and skill triggers,
    /// optionally filtered to skills that support the given agent role.
    /// </summary>
    public IReadOnlyList<SkillMatch> Match(string task, string? agentRole = null)
    {
        if (string.IsNullOrWhiteSpace(task))
            return [];

        var taskWords = Tokenize(task);
        var results = new List<SkillMatch>();

        foreach (var skill in _skills.Values)
        {
            // Filter by agent role when specified
            if (agentRole is not null
                && skill.AgentRoles.Count > 0
                && !skill.AgentRoles.Any(r => string.Equals(r, agentRole, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (skill.Triggers.Count == 0)
                continue;

            var matchedTriggers = skill.Triggers
                .Where(t => taskWords.Contains(t.ToLowerInvariant()))
                .ToList();

            if (matchedTriggers.Count == 0)
                continue;

            double score = (double)matchedTriggers.Count / skill.Triggers.Count;

            // Boost score for high-confidence skills
            score *= skill.Confidence switch
            {
                SkillConfidence.High => 1.2,
                SkillConfidence.Low  => 0.8,
                _                   => 1.0
            };

            results.Add(new SkillMatch
            {
                Skill = skill,
                Score = Math.Min(score, 1.0),
                Reason = $"Matched triggers: {string.Join(", ", matchedTriggers)}"
            });
        }

        return results.OrderByDescending(m => m.Score).ToList().AsReadOnly();
    }

    /// <summary>Returns the Content field of the skill, or null if not found.</summary>
    public string? LoadContent(string skillId) =>
        _skills.TryGetValue(skillId, out var skill) ? skill.Content : null;

    private static HashSet<string> Tokenize(string text) =>
        text.ToLowerInvariant()
            .Split([' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':'], StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();
}
