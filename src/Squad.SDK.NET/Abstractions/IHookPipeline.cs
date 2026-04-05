using Squad.SDK.NET.Hooks;

namespace Squad.SDK.NET.Abstractions;

/// <summary>
/// Pipeline for intercepting tool invocations with pre- and post-execution hooks.
/// </summary>
public interface IHookPipeline
{
    /// <summary>
    /// Registers a hook that runs before a tool is executed.
    /// </summary>
    /// <param name="hook">A function that inspects or modifies the tool invocation before execution.</param>
    void AddPreToolHook(Func<PreToolUseContext, Task<PreToolUseResult>> hook);

    /// <summary>
    /// Registers a hook that runs after a tool has executed.
    /// </summary>
    /// <param name="hook">A function that inspects or modifies the tool result after execution.</param>
    void AddPostToolHook(Func<PostToolUseContext, Task<PostToolUseResult>> hook);

    /// <summary>
    /// Runs all registered pre-tool hooks in order.
    /// </summary>
    /// <param name="context">The context describing the pending tool invocation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="PreToolUseResult"/> indicating whether the tool should proceed.</returns>
    Task<PreToolUseResult> RunPreToolHooksAsync(PreToolUseContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs all registered post-tool hooks in order.
    /// </summary>
    /// <param name="context">The context describing the completed tool invocation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="PostToolUseResult"/> with the potentially modified result.</returns>
    Task<PostToolUseResult> RunPostToolHooksAsync(PostToolUseContext context, CancellationToken cancellationToken = default);
}
