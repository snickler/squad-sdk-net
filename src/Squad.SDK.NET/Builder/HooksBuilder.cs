using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Builder;

public sealed class HooksBuilder
{
    private readonly List<string> _allowedWritePaths = [];
    private readonly List<string> _blockedCommands = [];
    private int? _maxAskUser;
    private bool _scrubPii;
    private bool _reviewerLockout;

    public HooksBuilder AllowedWritePaths(params string[] paths) { _allowedWritePaths.AddRange(paths); return this; }
    public HooksBuilder BlockedCommands(params string[] commands) { _blockedCommands.AddRange(commands); return this; }
    public HooksBuilder MaxAskUser(int max) { _maxAskUser = max; return this; }
    public HooksBuilder ScrubPii(bool enabled = true) { _scrubPii = enabled; return this; }
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
