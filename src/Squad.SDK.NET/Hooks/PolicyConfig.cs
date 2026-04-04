namespace Squad.SDK.NET.Hooks;

public sealed record PolicyConfig
{
    public IReadOnlyList<string>? AllowedWritePaths { get; init; }
    public IReadOnlyList<string>? BlockedCommands { get; init; }
    public int? MaxAskUserPerSession { get; init; }
    public bool ScrubPii { get; init; }
    public bool ReviewerLockout { get; init; }
}
