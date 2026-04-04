namespace Squad.SDK.NET.Agents;

public static class CharterCompiler
{
    public static async Task<AgentCharter> CompileAsync(string charterPath, CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(charterPath, cancellationToken);
        return Parse(content);
    }

    public static async Task<IReadOnlyList<AgentCharter>> CompileAllAsync(string teamRoot, CancellationToken cancellationToken = default)
    {
        var charterFiles = Directory.GetFiles(teamRoot, "charter.md", SearchOption.AllDirectories);
        var results = new List<AgentCharter>(charterFiles.Length);

        foreach (var file in charterFiles)
        {
            results.Add(await CompileAsync(file, cancellationToken));
        }

        return results;
    }

    private static AgentCharter Parse(string content)
    {
        var frontmatter = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string prompt = string.Empty;

        var lines = content.ReplaceLineEndings("\n").Split('\n');
        int i = 0;

        // Skip leading blank lines
        while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i])) i++;

        if (i < lines.Length && lines[i].Trim() == "---")
        {
            i++; // consume opening ---
            var frontmatterLines = new List<string>();

            while (i < lines.Length && lines[i].Trim() != "---")
            {
                frontmatterLines.Add(lines[i]);
                i++;
            }

            if (i < lines.Length) i++; // consume closing ---

            foreach (var line in frontmatterLines)
            {
                var colonIdx = line.IndexOf(':');
                if (colonIdx < 0) continue;

                var key = line[..colonIdx].Trim();
                var value = line[(colonIdx + 1)..].Trim();
                frontmatter[key] = value;
            }
        }

        // Remaining lines are the prompt body
        prompt = string.Join('\n', lines[i..]).Trim();

        return new AgentCharter
        {
            Name = frontmatter.GetValueOrDefault("name") ?? Path.GetDirectoryName("") ?? "unknown",
            DisplayName = frontmatter.GetValueOrDefault("displayName"),
            Role = frontmatter.GetValueOrDefault("role") ?? "agent",
            Expertise = ParseArray(frontmatter.GetValueOrDefault("expertise")),
            Style = frontmatter.GetValueOrDefault("style"),
            Prompt = prompt,
            AllowedTools = ParseArray(frontmatter.GetValueOrDefault("allowedTools")),
            ExcludedTools = ParseArray(frontmatter.GetValueOrDefault("excludedTools")),
            ModelPreference = frontmatter.GetValueOrDefault("modelPreference"),
        };
    }

    private static IReadOnlyList<string> ParseArray(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];

        // Handle [item1, item2, item3] format
        value = value.Trim();
        if (value.StartsWith('[') && value.EndsWith(']'))
        {
            value = value[1..^1];
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.Trim('"', '\''))
            .ToList();
    }
}
