namespace Squad.SDK.NET.Hooks;

/// <summary>
/// Represents the outcome of a pre-tool-use hook, indicating whether the tool call should proceed, be blocked, or have its arguments modified.
/// </summary>
/// <seealso cref="PreToolUseContext"/>
/// <seealso cref="HookAction"/>
public sealed record PreToolUseResult
{
    /// <summary>Gets the action the hook pipeline should take.</summary>
    public required HookAction Action { get; init; }

    /// <summary>Gets modified arguments when <see cref="Action"/> is <see cref="HookAction.Modify"/>; otherwise, <see langword="null"/>.</summary>
    public IReadOnlyDictionary<string, object?>? ModifiedArguments { get; init; }

    /// <summary>Gets an optional reason explaining a <see cref="HookAction.Block"/> decision.</summary>
    public string? Reason { get; init; }

    /// <summary>Creates a result that allows the tool call to proceed unchanged.</summary>
    /// <returns>A <see cref="PreToolUseResult"/> with <see cref="HookAction.Allow"/>.</returns>
    public static PreToolUseResult Allow() => new() { Action = HookAction.Allow };

    /// <summary>Creates a result that blocks the tool call.</summary>
    /// <param name="reason">The reason the tool call was blocked.</param>
    /// <returns>A <see cref="PreToolUseResult"/> with <see cref="HookAction.Block"/>.</returns>
    public static PreToolUseResult Block(string reason) => new() { Action = HookAction.Block, Reason = reason };

    /// <summary>Creates a result that modifies the tool call arguments.</summary>
    /// <param name="args">The replacement arguments to use.</param>
    /// <returns>A <see cref="PreToolUseResult"/> with <see cref="HookAction.Modify"/>.</returns>
    public static PreToolUseResult Modify(IReadOnlyDictionary<string, object?> args) =>
        new() { Action = HookAction.Modify, ModifiedArguments = args };
}

/// <summary>
/// Specifies the action a pre-tool-use hook instructs the pipeline to take.
/// </summary>
public enum HookAction
{
    /// <summary>Allow the tool call to proceed.</summary>
    Allow,

    /// <summary>Block the tool call from executing.</summary>
    Block,

    /// <summary>Allow the tool call but with modified arguments.</summary>
    Modify
}
