using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Casting;

/// <summary>
/// Manages agent casting across universes, assigning personas and traits to agents.
/// </summary>
public sealed class CastingEngine
{
    private readonly ConcurrentDictionary<string, CastingRecord> _casts = new();
    private readonly ILogger<CastingEngine> _logger;
    private CastingConfig _config;

    /// <summary>
    /// Initializes a new <see cref="CastingEngine"/> with optional configuration.
    /// </summary>
    /// <param name="config">Casting configuration, or <see langword="null"/> for defaults.</param>
    /// <param name="logger">Logger instance.</param>
    public CastingEngine(CastingConfig? config, ILogger<CastingEngine> logger)
    {
        _config = config ?? new CastingConfig();
        _logger = logger;
    }

    /// <summary>Replaces the current casting configuration.</summary>
    /// <param name="config">The new configuration to use.</param>
    public void UpdateConfig(CastingConfig config)
    {
        _config = config;
    }

    /// <summary>Casts an agent into a role within a universe, returning the resulting <see cref="CastMember"/>.</summary>
    /// <param name="agentName">Name of the agent to cast.</param>
    /// <param name="roleId">The role identifier to assign.</param>
    /// <param name="preferredUniverse">Optional preferred universe; a random one is selected if not specified or not allowed.</param>
    /// <returns>The <see cref="CastMember"/> representing the cast agent.</returns>
    /// <exception cref="InvalidOperationException">Thrown when capacity is reached and overflow strategy is <see cref="OverflowStrategy.Reject"/>.</exception>
    /// <exception cref="NotSupportedException">Thrown when <see cref="OverflowStrategy.Queue"/> is configured (not implemented).</exception>
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

    /// <summary>Returns the casting record for the given agent, or <see langword="null"/> if not found.</summary>
    /// <param name="agentName">The agent name to look up.</param>
    /// <returns>The <see cref="CastingRecord"/> if found; otherwise <see langword="null"/>.</returns>
    public CastingRecord? GetCast(string agentName)
    {
        _casts.TryGetValue(agentName, out var record);
        return record;
    }

    /// <summary>Returns a snapshot of all current casting records.</summary>
    /// <returns>A read-only list of all <see cref="CastingRecord"/> instances.</returns>
    public IReadOnlyList<CastingRecord> GetAllCasts() =>
        _casts.Values.ToList().AsReadOnly();

    /// <summary>Removes the casting record for the specified agent.</summary>
    /// <param name="agentName">The agent name whose cast to remove.</param>
    public void RemoveCast(string agentName)
    {
        _casts.TryRemove(agentName, out _);
    }

    /// <summary>Removes all casting records.</summary>
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
