using Squad.SDK.NET.Hooks;

namespace Squad.SDK.NET.Config;

/// <summary>Defines policy hooks that enforce safety and governance rules for agent sessions.</summary>
/// <seealso cref="PolicyConfig"/>
public sealed record HooksDefinition
{
    /// <summary>Gets the glob patterns for file paths that agents are allowed to write to.</summary>
    public IReadOnlyList<string>? AllowedWritePaths { get; init; }

    /// <summary>Gets the list of shell commands that agents are forbidden from executing.</summary>
    public IReadOnlyList<string>? BlockedCommands { get; init; }

    /// <summary>Gets the maximum number of times an agent may prompt the user per session.</summary>
    public int? MaxAskUserPerSession { get; init; }

    /// <summary>Gets a value indicating whether personally identifiable information should be scrubbed from output.</summary>
    public bool ScrubPii { get; init; }

    /// <summary>Gets a value indicating whether a reviewer lockout is enforced, preventing the author from self-approving.</summary>
    public bool ReviewerLockout { get; init; }

    /// <summary>Converts this definition into a <see cref="PolicyConfig"/> instance.</summary>
    /// <returns>A new <see cref="PolicyConfig"/> populated from this definition.</returns>
    public PolicyConfig ToPolicyConfig() => new()
    {
        AllowedWritePaths = AllowedWritePaths,
        BlockedCommands = BlockedCommands,
        MaxAskUserPerSession = MaxAskUserPerSession,
        ScrubPii = ScrubPii,
        ReviewerLockout = ReviewerLockout
    };
}
