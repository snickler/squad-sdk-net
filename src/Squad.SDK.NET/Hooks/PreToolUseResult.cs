namespace Squad.SDK.NET.Hooks;

public sealed record PreToolUseResult
{
    public required HookAction Action { get; init; }
    public IReadOnlyDictionary<string, object?>? ModifiedArguments { get; init; }
    public string? Reason { get; init; }

    public static PreToolUseResult Allow() => new() { Action = HookAction.Allow };
    public static PreToolUseResult Block(string reason) => new() { Action = HookAction.Block, Reason = reason };
    public static PreToolUseResult Modify(IReadOnlyDictionary<string, object?> args) =>
        new() { Action = HookAction.Modify, ModifiedArguments = args };
}

public enum HookAction { Allow, Block, Modify }
