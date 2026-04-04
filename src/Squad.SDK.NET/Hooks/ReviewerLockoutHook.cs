using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Squad.SDK.NET.Hooks;

/// <summary>
/// A pre-tool-use hook that prevents agents from modifying artifacts they have been locked out of, enforcing reviewer separation of concerns.
/// </summary>
/// <seealso cref="PreToolUseContext"/>
/// <seealso cref="LockoutRecord"/>
public sealed class ReviewerLockoutHook
{
    private readonly ConcurrentDictionary<string, LockoutRecord> _lockouts = new();
    private readonly ILogger<ReviewerLockoutHook> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReviewerLockoutHook"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ReviewerLockoutHook(ILogger<ReviewerLockoutHook> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Locks out the specified agent from modifying the given artifact.
    /// </summary>
    /// <param name="artifactId">The identifier of the artifact to lock (typically a file path).</param>
    /// <param name="agentName">The name of the agent to lock out.</param>
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

    /// <summary>
    /// Determines whether the specified agent is locked out from the given artifact.
    /// </summary>
    /// <param name="artifactId">The artifact identifier to check.</param>
    /// <param name="agentName">The agent name to check.</param>
    /// <returns><see langword="true"/> if the agent is locked out; otherwise, <see langword="false"/>.</returns>
    public bool IsLockedOut(string artifactId, string agentName)
    {
        return _lockouts.TryGetValue(artifactId, out var record)
            && record.AgentName == agentName;
    }

    /// <summary>
    /// Removes the lockout for the specified artifact.
    /// </summary>
    /// <param name="artifactId">The artifact identifier to unlock.</param>
    public void ClearLockout(string artifactId)
    {
        if (_lockouts.TryRemove(artifactId, out _))
            _logger.LogInformation("Cleared lockout for artifact '{Artifact}'", artifactId);
    }

    /// <summary>
    /// Returns a dictionary of all currently locked artifacts mapped to the agent name that is locked out.
    /// </summary>
    /// <returns>A read-only dictionary keyed by artifact identifier with agent names as values.</returns>
    public IReadOnlyDictionary<string, string> GetLockedAgents()
    {
        return _lockouts.ToDictionary(kv => kv.Key, kv => kv.Value.AgentName);
    }

    /// <summary>
    /// Removes all active lockouts.
    /// </summary>
    public void ClearAll()
    {
        _lockouts.Clear();
        _logger.LogInformation("Cleared all lockouts");
    }

    /// <summary>
    /// Creates a pre-tool-use hook delegate that blocks tool calls targeting locked artifacts.
    /// </summary>
    /// <returns>A delegate suitable for registration with <see cref="HookPipeline.AddPreToolHook"/>.</returns>
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

/// <summary>
/// Represents a lockout entry associating an artifact with the agent that is prevented from modifying it.
/// </summary>
public sealed record LockoutRecord
{
    /// <summary>Gets the identifier of the locked artifact.</summary>
    public required string ArtifactId { get; init; }

    /// <summary>Gets the name of the agent that is locked out.</summary>
    public required string AgentName { get; init; }

    /// <summary>Gets the UTC timestamp when the lockout was established.</summary>
    public DateTimeOffset LockedAt { get; init; }
}
