using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Casting;

public sealed class CastingEngine
{
    private readonly ConcurrentDictionary<string, CastingRecord> _casts = new();
    private readonly ILogger<CastingEngine> _logger;
    private CastingConfig _config;

    public CastingEngine(CastingConfig? config, ILogger<CastingEngine> logger)
    {
        _config = config ?? new CastingConfig();
        _logger = logger;
    }

    public void UpdateConfig(CastingConfig config)
    {
        _config = config;
    }

    public CastMember Cast(string agentName, string roleId, string? preferredUniverse = null)
    {
        var universe = SelectUniverse(preferredUniverse);

        if (_config.Capacity.HasValue && _casts.Count >= _config.Capacity.Value)
        {
            switch (_config.OverflowStrategy)
            {
                case OverflowStrategy.Reject:
                    throw new InvalidOperationException($"Casting capacity ({_config.Capacity.Value}) reached.");
                case OverflowStrategy.Rotate:
                    var oldest = _casts.Values.OrderBy(c => c.AssignedAt).FirstOrDefault();
                    if (oldest is not null)
                    {
                        _casts.TryRemove(oldest.AgentName, out _);
                        _logger.LogInformation("Rotated out cast for agent '{Agent}'", oldest.AgentName);
                    }
                    break;
                case OverflowStrategy.Queue:
                    _logger.LogWarning("Casting capacity reached, but queueing is not implemented for agent '{Agent}'", agentName);
                    throw new NotSupportedException("OverflowStrategy.Queue is configured, but queueing is not implemented.");
            }
        }

        var member = new CastMember
        {
            Name = $"{agentName}-{universe}",
            Persona = $"Agent {agentName} cast from {universe}",
            Universe = universe,
            Traits = [$"role:{roleId}", $"universe:{universe}"]
        };

        var record = new CastingRecord
        {
            AgentName = agentName,
            Member = member,
            RoleId = roleId
        };

        _casts[agentName] = record;
        _logger.LogInformation("Cast agent '{Agent}' as '{Persona}' from universe '{Universe}'",
            agentName, member.Name, universe);

        return member;
    }

    public CastingRecord? GetCast(string agentName)
    {
        _casts.TryGetValue(agentName, out var record);
        return record;
    }

    public IReadOnlyList<CastingRecord> GetAllCasts() =>
        _casts.Values.ToList().AsReadOnly();

    public void RemoveCast(string agentName)
    {
        _casts.TryRemove(agentName, out _);
    }

    public void ClearAll()
    {
        _casts.Clear();
    }

    private string SelectUniverse(string? preferred)
    {
        if (preferred is not null && (_config.AllowlistUniverses.Count == 0 ||
            _config.AllowlistUniverses.Contains(preferred, StringComparer.OrdinalIgnoreCase)))
        {
            return preferred;
        }

        if (_config.AllowlistUniverses.Count > 0)
            return _config.AllowlistUniverses[Random.Shared.Next(_config.AllowlistUniverses.Count)];

        return "default";
    }
}
