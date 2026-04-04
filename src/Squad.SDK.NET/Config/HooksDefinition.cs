using Squad.SDK.NET.Hooks;

namespace Squad.SDK.NET.Config;

public sealed record HooksDefinition
{
    public IReadOnlyList<string>? AllowedWritePaths { get; init; }
    public IReadOnlyList<string>? BlockedCommands { get; init; }
    public int? MaxAskUserPerSession { get; init; }
    public bool ScrubPii { get; init; }
    public bool ReviewerLockout { get; init; }

    public PolicyConfig ToPolicyConfig() => new()
    {
        AllowedWritePaths = AllowedWritePaths,
        BlockedCommands = BlockedCommands,
        MaxAskUserPerSession = MaxAskUserPerSession,
        ScrubPii = ScrubPii,
        ReviewerLockout = ReviewerLockout
    };
}
