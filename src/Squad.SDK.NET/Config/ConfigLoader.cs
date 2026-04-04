using System.Text.Json;
using System.Text.Json.Serialization;

namespace Squad.SDK.NET.Config;

public static class ConfigLoader
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Loads a <see cref="SquadConfig"/> from a JSON file asynchronously.</summary>
    public static async Task<SquadConfig> LoadAsync(string filePath, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        var config = await JsonSerializer.DeserializeAsync<SquadConfig>(stream, DefaultOptions, ct)
            .ConfigureAwait(false);

        return config ?? throw new InvalidOperationException($"Failed to deserialize SquadConfig from '{filePath}'.");
    }

    /// <summary>Loads a <see cref="SquadConfig"/> from a JSON file synchronously.</summary>
    public static SquadConfig LoadSync(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<SquadConfig>(json, DefaultOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize SquadConfig from '{filePath}'.");
    }

    /// <summary>Validates a <see cref="SquadConfig"/> and returns a list of validation errors.</summary>
    public static IReadOnlyList<string> Validate(SquadConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.Team.Name))
            errors.Add("Team.Name is required.");

        foreach (var agent in config.Agents)
        {
            if (string.IsNullOrWhiteSpace(agent.Name))
                errors.Add("An agent is missing a Name.");

            if (string.IsNullOrWhiteSpace(agent.Role))
                errors.Add($"Agent '{agent.Name}' is missing a Role.");
        }

        if (config.Routing is { } routing)
        {
            var agentNames = config.Agents.Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var rule in routing.Rules)
            {
                if (string.IsNullOrWhiteSpace(rule.WorkType))
                    errors.Add("A routing rule is missing a WorkType.");

                if (rule.Agents.Count == 0)
                    errors.Add($"Routing rule '{rule.WorkType}' has no agents.");

                foreach (var agentRef in rule.Agents)
                {
                    if (!agentNames.Contains(agentRef))
                        errors.Add($"Routing rule '{rule.WorkType}' references unknown agent '{agentRef}'.");
                }
            }

            if (routing.DefaultAgent is { } defaultAgent && !agentNames.Contains(defaultAgent))
                errors.Add($"Routing DefaultAgent '{defaultAgent}' does not match any defined agent.");
        }

        return errors.AsReadOnly();
    }
}
