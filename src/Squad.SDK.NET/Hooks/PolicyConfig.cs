namespace Squad.SDK.NET.Hooks;

/// <summary>
/// Declarative policy configuration for the <see cref="HookPipeline"/>, enabling built-in safety constraints.
/// </summary>
/// <seealso cref="HookPipeline"/>
public sealed record PolicyConfig
{
    /// <summary>Gets the list of file path prefixes that agents are allowed to write to.</summary>
    public IReadOnlyList<string>? AllowedWritePaths { get; init; }

    /// <summary>Gets the list of shell command substrings that are blocked from execution.</summary>
    public IReadOnlyList<string>? BlockedCommands { get; init; }

    /// <summary>Gets the maximum number of <c>ask_user</c> tool calls allowed per session, or <see langword="null"/> for unlimited.</summary>
    public int? MaxAskUserPerSession { get; init; }

    /// <summary>Gets a value indicating whether PII scrubbing is enabled on tool outputs.</summary>
    public bool ScrubPii { get; init; }

    /// <summary>Gets a value indicating whether reviewer lockout enforcement is enabled.</summary>
    public bool ReviewerLockout { get; init; }
}
