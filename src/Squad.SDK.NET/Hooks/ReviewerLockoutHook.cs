using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Squad.SDK.NET.Hooks;

public sealed class ReviewerLockoutHook
{
    private readonly ConcurrentDictionary<string, LockoutRecord> _lockouts = new();
    private readonly ILogger<ReviewerLockoutHook> _logger;

    public ReviewerLockoutHook(ILogger<ReviewerLockoutHook> logger)
    {
        _logger = logger;
    }

    public void Lockout(string artifactId, string agentName)
    {
        var record = new LockoutRecord
        {
            ArtifactId = artifactId,
            AgentName = agentName,
            LockedAt = DateTimeOffset.UtcNow
        };
        _lockouts[artifactId] = record;
        _logger.LogInformation("Locked out agent '{Agent}' from artifact '{Artifact}'", agentName, artifactId);
    }

    public bool IsLockedOut(string artifactId, string agentName)
    {
        return _lockouts.TryGetValue(artifactId, out var record)
            && record.AgentName == agentName;
    }

    public void ClearLockout(string artifactId)
    {
        if (_lockouts.TryRemove(artifactId, out _))
            _logger.LogInformation("Cleared lockout for artifact '{Artifact}'", artifactId);
    }

    public IReadOnlyDictionary<string, string> GetLockedAgents()
    {
        return _lockouts.ToDictionary(kv => kv.Key, kv => kv.Value.AgentName);
    }

    public void ClearAll()
    {
        _lockouts.Clear();
        _logger.LogInformation("Cleared all lockouts");
    }

    public Func<PreToolUseContext, Task<PreToolUseResult>> CreateHook()
    {
        return context =>
        {
            // Check if the tool writes to a locked artifact
            if (context.Arguments.TryGetValue("path", out var pathObj) && pathObj is string path)
            {
                if (IsLockedOut(path, context.AgentName))
                {
                    _logger.LogWarning("Agent '{Agent}' blocked from locked artifact '{Artifact}'",
                        context.AgentName, path);
                    return Task.FromResult(PreToolUseResult.Block(
                        $"Artifact '{path}' is locked out for agent '{context.AgentName}'."));
                }
            }
            return Task.FromResult(PreToolUseResult.Allow());
        };
    }
}

public sealed record LockoutRecord
{
    public required string ArtifactId { get; init; }
    public required string AgentName { get; init; }
    public DateTimeOffset LockedAt { get; init; }
}
