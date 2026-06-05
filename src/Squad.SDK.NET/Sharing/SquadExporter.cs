using System.Text.Json;
using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Sharing;

/// <summary>
/// Exports <see cref="SquadConfig"/> instances to a portable JSON format.
/// </summary>
public sealed class SquadExporter
{
    private readonly ILogger<SquadExporter> _logger;

    /// <summary>
    /// Initializes a new <see cref="SquadExporter"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public SquadExporter(ILogger<SquadExporter> logger)
    {
        _logger = logger;
    }

    /// <summary>Exports a squad configuration to an <see cref="ExportedSquad"/> instance.</summary>
    /// <param name="config">The squad configuration to export.</param>
    /// <param name="author">Optional author attribution.</param>
    /// <param name="topLevelFiles">
    /// Optional top-level squad files to preserve in the export payload
    /// (for example <c>team.md</c>, <c>decisions.md</c>, and <c>routing.md</c>).
    /// Null values are normalized to empty strings to preserve file existence.
    /// </param>
    /// <returns>An <see cref="ExportedSquad"/> containing the serialized configuration and agent list.</returns>
    public ExportedSquad Export(
        SquadConfig config,
        string? author = null,
        IReadOnlyDictionary<string, string?>? topLevelFiles = null)
    {
        var configJson = JsonSerializer.Serialize(config, SharingJsonContext.Default.SquadConfig);
        var normalizedTopLevelFiles = NormalizeTopLevelFiles(topLevelFiles);

        var agents = config.Agents.Select(a => new ExportedAgent
        {
            Name = a.Name,
            Role = a.Role,
            Charter = a.Charter,
            Prompt = a.Prompt
        }).ToList();

        _logger.LogInformation("Exported squad '{Name}' with {AgentCount} agents",
            config.Team.Name, agents.Count);

        return new ExportedSquad
        {
            Name = config.Team.Name,
            Version = config.Version,
            Description = config.Team.Description,
            Author = author,
            ConfigJson = configJson,
            Agents = agents.AsReadOnly(),
            TopLevelFiles = normalizedTopLevelFiles
        };
    }

    /// <summary>Exports a squad configuration to a JSON file on disk.</summary>
    /// <param name="config">The squad configuration to export.</param>
    /// <param name="filePath">Destination file path.</param>
    /// <param name="author">Optional author attribution.</param>
    /// <param name="topLevelFiles">
    /// Optional top-level squad files to include in the export payload.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ExportToFileAsync(
        SquadConfig config,
        string filePath,
        string? author = null,
        IReadOnlyDictionary<string, string?>? topLevelFiles = null,
        CancellationToken cancellationToken = default)
    {
        var exported = Export(config, author, topLevelFiles);
        var json = JsonSerializer.Serialize(exported, SharingJsonContext.Default.ExportedSquad);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
        _logger.LogInformation("Exported squad to {Path}", filePath);
    }

    private static IReadOnlyDictionary<string, string> NormalizeTopLevelFiles(
        IReadOnlyDictionary<string, string?>? topLevelFiles)
    {
        if (topLevelFiles is null || topLevelFiles.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in topLevelFiles)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            normalized[key] = value ?? string.Empty;
        }

        return normalized;
    }
}
