using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Builder;

/// <summary>
/// Fluent builder for configuring a <see cref="HooksDefinition"/>.
/// </summary>
/// <seealso cref="SquadBuilder"/>
public sealed class HooksBuilder
{
    private readonly List<string> _allowedWritePaths = [];
    private readonly List<string> _blockedCommands = [];
    private int? _maxAskUser;
    private bool _scrubPii;
    private bool _reviewerLockout;

    /// <summary>Adds file paths that agents are allowed to write to.</summary>
    /// <param name="paths">Allowed write paths.</param>
    /// <returns>This builder instance for chaining.</returns>
    public HooksBuilder AllowedWritePaths(params string[] paths) { _allowedWritePaths.AddRange(paths); return this; }

    /// <summary>Adds commands that agents are blocked from executing.</summary>
    /// <param name="commands">Blocked command patterns.</param>
    /// <returns>This builder instance for chaining.</returns>
    public HooksBuilder BlockedCommands(params string[] commands) { _blockedCommands.AddRange(commands); return this; }

    /// <summary>Sets the maximum number of user prompts allowed per session.</summary>
    /// <param name="max">Maximum ask-user count.</param>
    /// <returns>This builder instance for chaining.</returns>
    public HooksBuilder MaxAskUser(int max) { _maxAskUser = max; return this; }

    /// <summary>Enables or disables PII scrubbing in tool outputs.</summary>
    /// <param name="enabled"><see langword="true"/> to enable PII scrubbing.</param>
    /// <returns>This builder instance for chaining.</returns>
    public HooksBuilder ScrubPii(bool enabled = true) { _scrubPii = enabled; return this; }

    /// <summary>Enables or disables reviewer lockout for code review workflows.</summary>
    /// <param name="enabled"><see langword="true"/> to enable reviewer lockout.</param>
    /// <returns>This builder instance for chaining.</returns>
    public HooksBuilder ReviewerLockout(bool enabled = true) { _reviewerLockout = enabled; return this; }

    internal HooksDefinition Build() => new()
    {
        AllowedWritePaths = _allowedWritePaths.Count > 0 ? _allowedWritePaths.AsReadOnly() : null,
        BlockedCommands = _blockedCommands.Count > 0 ? _blockedCommands.AsReadOnly() : null,
        MaxAskUserPerSession = _maxAskUser,
        ScrubPii = _scrubPii,
        ReviewerLockout = _reviewerLockout
    };
}
