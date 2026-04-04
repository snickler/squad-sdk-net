using System.Text.Json;
using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Sharing;

public sealed class SquadExporter
{
    private readonly ILogger<SquadExporter> _logger;

    public SquadExporter(ILogger<SquadExporter> logger)
    {
        _logger = logger;
    }

    public ExportedSquad Export(SquadConfig config, string? author = null)
    {
        var configJson = JsonSerializer.Serialize(config, SharingJsonContext.Default.SquadConfig);

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
            Agents = agents.AsReadOnly()
        };
    }

    public async Task ExportToFileAsync(SquadConfig config, string filePath, string? author = null, CancellationToken cancellationToken = default)
    {
        var exported = Export(config, author);
        var json = JsonSerializer.Serialize(exported, SharingJsonContext.Default.ExportedSquad);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
        _logger.LogInformation("Exported squad to {Path}", filePath);
    }
}
