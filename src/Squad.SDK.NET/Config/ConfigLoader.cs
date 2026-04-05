using System.Text.Json;

namespace Squad.SDK.NET.Config;

/// <summary>Provides methods for loading and validating <see cref="SquadConfig"/> from JSON files.</summary>
public static class ConfigLoader
{
    /// <summary>Loads a <see cref="SquadConfig"/> from a JSON file asynchronously.</summary>
    /// <param name="filePath">The path to the JSON configuration file.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The deserialized <see cref="SquadConfig"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when deserialization returns <see langword="null"/>.</exception>
    public static async Task<SquadConfig> LoadAsync(string filePath, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        var config = await JsonSerializer.DeserializeAsync(stream, ConfigJsonContext.Default.SquadConfig, ct)
            .ConfigureAwait(false);

        return config ?? throw new InvalidOperationException($"Failed to deserialize SquadConfig from '{filePath}'.");
    }

    /// <summary>Loads a <see cref="SquadConfig"/> from a JSON file synchronously.</summary>
    /// <param name="filePath">The path to the JSON configuration file.</param>
    /// <returns>The deserialized <see cref="SquadConfig"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when deserialization returns <see langword="null"/>.</exception>
    public static SquadConfig LoadSync(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize(json, ConfigJsonContext.Default.SquadConfig)
            ?? throw new InvalidOperationException($"Failed to deserialize SquadConfig from '{filePath}'.");
    }

    /// <summary>Validates a <see cref="SquadConfig"/> and returns a list of validation errors.</summary>
    /// <param name="config">The configuration to validate.</param>
    /// <returns>A read-only list of validation error messages; empty if valid.</returns>
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
