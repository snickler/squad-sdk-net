namespace Squad.SDK.NET.Skills;

public static class SkillLoader
{
    /// <summary>Loads a single SKILL.md file with YAML frontmatter.</summary>
    public static async Task<SkillDefinition> LoadAsync(string filePath, CancellationToken ct = default)
    {
        var content = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
        return Parse(content, filePath);
    }

    /// <summary>Recursively loads all SKILL.md files from a directory.</summary>
    public static async Task<IReadOnlyList<SkillDefinition>> LoadDirectoryAsync(
        string directoryPath,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(directoryPath))
            return [];

        var files = Directory.EnumerateFiles(directoryPath, "SKILL.md", SearchOption.AllDirectories);
        var tasks = files.Select(f => LoadAsync(f, ct));
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.ToList().AsReadOnly();
    }

    private static SkillDefinition Parse(string raw, string filePath)
    {
        var (frontmatter, body) = SplitFrontmatter(raw);

        var id = frontmatter.GetValueOrDefault("id")
            ?? Path.GetFileNameWithoutExtension(filePath);
        var name = frontmatter.GetValueOrDefault("name") ?? id;
        var triggers = ParseList(frontmatter.GetValueOrDefault("triggers"));
        var agentRoles = ParseList(frontmatter.GetValueOrDefault("agentRoles"));
        var confidence = ParseConfidence(frontmatter.GetValueOrDefault("confidence"));

        return new SkillDefinition
        {
            Id = id,
            Name = name,
            Triggers = triggers,
            AgentRoles = agentRoles,
            Content = body,
            Confidence = confidence
        };
    }

    private static (Dictionary<string, string> frontmatter, string body) SplitFrontmatter(string content)
    {
        var frontmatter = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!content.StartsWith("---", StringComparison.Ordinal))
            return (frontmatter, content);

        // Find the closing ---
        var closeIndex = content.IndexOf("---", 3, StringComparison.Ordinal);
        if (closeIndex < 0)
            return (frontmatter, content);

        var yamlBlock = content[3..closeIndex].Trim();
        var body = content[(closeIndex + 3)..].TrimStart('\r', '\n');

        foreach (var line in yamlBlock.Split('\n'))
        {
            var trimmed = line.Trim();
            var colon = trimmed.IndexOf(':');
            if (colon < 0) continue;

            var key = trimmed[..colon].Trim();
            var value = trimmed[(colon + 1)..].Trim();
            if (key.Length > 0)
                frontmatter[key] = value;
        }

        return (frontmatter, body);
    }

    private static IReadOnlyList<string> ParseList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var trimmed = value.Trim();

        // Inline array: [item1, item2, item3]
        if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
        {
            return trimmed[1..^1]
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList()
                .AsReadOnly();
        }

        // Single value
        return [trimmed];
    }

    private static SkillConfidence ParseConfidence(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "low"  => SkillConfidence.Low,
            "high" => SkillConfidence.High,
            _      => SkillConfidence.Medium
        };
}
