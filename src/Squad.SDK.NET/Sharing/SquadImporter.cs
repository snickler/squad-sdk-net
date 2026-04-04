using System.Text.Json;
using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Sharing;

public sealed class SquadImporter
{
    private readonly ILogger<SquadImporter> _logger;

    public SquadImporter(ILogger<SquadImporter> logger)
    {
        _logger = logger;
    }

    public async Task<ImportResult> ImportFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return new ImportResult { Success = false, Message = $"File not found: {filePath}" };

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var exported = JsonSerializer.Deserialize(json, SharingJsonContext.Default.ExportedSquad);

            if (exported is null)
                return new ImportResult { Success = false, Message = "Failed to deserialize exported squad." };

            _logger.LogInformation("Imported squad '{Name}' v{Version} with {AgentCount} agents",
                exported.Name, exported.Version, exported.Agents.Count);

            return new ImportResult
            {
                Success = true,
                Message = $"Successfully imported squad '{exported.Name}'",
                ImportedPath = filePath
            };
        }
        catch (JsonException ex)
        {
            return new ImportResult { Success = false, Message = $"JSON parse error: {ex.Message}" };
        }
    }

    public SquadConfig? DeserializeConfig(ExportedSquad exported)
    {
        try
        {
            return JsonSerializer.Deserialize(exported.ConfigJson, SharingJsonContext.Default.SquadConfig);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize squad config from export");
            return null;
        }
    }
}
