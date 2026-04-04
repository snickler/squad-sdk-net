using System.Collections.Concurrent;

namespace Squad.SDK.NET.Skills;

/// <summary>
/// Thread-safe registry for managing and matching <see cref="SkillDefinition"/> instances.
/// </summary>
public sealed class SkillRegistry
{
    private readonly ConcurrentDictionary<string, SkillDefinition> _skills = new();

    /// <summary>Registers a skill, replacing any existing skill with the same ID.</summary>
    /// <param name="skill">The skill definition to register.</param>
    public void Register(SkillDefinition skill) =>
        _skills[skill.Id] = skill;

    /// <summary>Removes a skill by its identifier.</summary>
    /// <param name="skillId">The skill identifier to remove.</param>
    /// <returns><see langword="true"/> if the skill was found and removed; otherwise <see langword="false"/>.</returns>
    public bool Unregister(string skillId) =>
        _skills.TryRemove(skillId, out _);

    /// <summary>Returns the skill with the given identifier, or <see langword="null"/> if not found.</summary>
    /// <param name="skillId">The skill identifier.</param>
    /// <returns>The <see cref="SkillDefinition"/> if found; otherwise <see langword="null"/>.</returns>
    public SkillDefinition? Get(string skillId) =>
        _skills.TryGetValue(skillId, out var skill) ? skill : null;

    /// <summary>Returns a snapshot of all registered skills.</summary>
    /// <returns>A read-only list of all <see cref="SkillDefinition"/> instances.</returns>
    public IReadOnlyList<SkillDefinition> GetAll() =>
        _skills.Values.ToList().AsReadOnly();

    /// <summary>
    /// Matches skills by keyword overlap between task description and skill triggers,
    /// optionally filtered to skills that support the given agent role.
    /// </summary>
    /// <param name="task">The task description to match against skill triggers.</param>
    /// <param name="agentRole">Optional agent role to filter skills by.</param>
    /// <returns>A list of <see cref="SkillMatch"/> results ordered by score descending.</returns>
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

    /// <summary>Returns the content of a skill, or <see langword="null"/> if not found.</summary>
    /// <param name="skillId">The skill identifier.</param>
    /// <returns>The skill content string, or <see langword="null"/>.</returns>
    public string? LoadContent(string skillId) =>
        _skills.TryGetValue(skillId, out var skill) ? skill.Content : null;

    private static HashSet<string> Tokenize(string text) =>
        text.ToLowerInvariant()
            .Split([' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':'], StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();
}
