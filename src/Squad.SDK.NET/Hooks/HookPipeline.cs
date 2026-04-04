using System.Collections.Concurrent;
using Squad.SDK.NET.Abstractions;

namespace Squad.SDK.NET.Hooks;

public sealed class HookPipeline : IHookPipeline
{
    private readonly List<Func<PreToolUseContext, Task<PreToolUseResult>>> _preHooks = [];
    private readonly List<Func<PostToolUseContext, Task<PostToolUseResult>>> _postHooks = [];
    private readonly PolicyConfig? _policy;

    // Per-session ask_user counters for MaxAskUserPerSession enforcement
    private readonly ConcurrentDictionary<string, int> _askUserCounts = new();

    public HookPipeline(PolicyConfig? policy = null)
    {
        _policy = policy;

        if (policy is not null)
        {
            if (policy.AllowedWritePaths is { Count: > 0 })
                _preHooks.Add(EnforceAllowedWritePathsAsync);

            if (policy.BlockedCommands is { Count: > 0 })
                _preHooks.Add(EnforceBlockedCommandsAsync);

            if (policy.MaxAskUserPerSession.HasValue)
                _preHooks.Add(EnforceMaxAskUserAsync);
        }
    }

    public void AddPreToolHook(Func<PreToolUseContext, Task<PreToolUseResult>> hook) =>
        _preHooks.Add(hook);

    public void AddPostToolHook(Func<PostToolUseContext, Task<PostToolUseResult>> hook) =>
        _postHooks.Add(hook);

    public async Task<PreToolUseResult> RunPreToolHooksAsync(
        PreToolUseContext context, CancellationToken cancellationToken = default)
    {
        var currentArgs = context.Arguments;

        foreach (var hook in _preHooks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Build context with (possibly modified) arguments for this hook
            var ctx = context with { Arguments = currentArgs };
            var result = await hook(ctx).ConfigureAwait(false);

            if (result.Action == HookAction.Block)
                return result;

            if (result.Action == HookAction.Modify && result.ModifiedArguments is not null)
                currentArgs = result.ModifiedArguments;
        }

        // Return Allow, carrying any accumulated argument modifications
        return currentArgs == context.Arguments
            ? PreToolUseResult.Allow()
            : PreToolUseResult.Modify(currentArgs);
    }

    public async Task<PostToolUseResult> RunPostToolHooksAsync(
        PostToolUseContext context, CancellationToken cancellationToken = default)
    {
        foreach (var hook in _postHooks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await hook(context).ConfigureAwait(false);
            if (!result.Success)
                return result;
        }

        return PostToolUseResult.Ok();
    }

    // ── Built-in policy hooks ────────────────────────────────────────────────

    private Task<PreToolUseResult> EnforceAllowedWritePathsAsync(PreToolUseContext context)
    {
        // Only applies to file-write tools (write_file, str_replace_editor, etc.)
        const string writeToolPrefix = "write";
        const string editToolPrefix = "str_replace";

        var isWriteTool =
            context.ToolName.StartsWith(writeToolPrefix, StringComparison.OrdinalIgnoreCase) ||
            context.ToolName.StartsWith(editToolPrefix, StringComparison.OrdinalIgnoreCase);

        if (!isWriteTool)
            return Task.FromResult(PreToolUseResult.Allow());

        if (context.Arguments.TryGetValue("path", out var pathObj) && pathObj is string path)
        {
            var allowed = _policy!.AllowedWritePaths!;
            var isAllowed = allowed.Any(allowed =>
                path.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
                return Task.FromResult(
                    PreToolUseResult.Block($"Write to '{path}' is not within allowed paths."));
        }

        return Task.FromResult(PreToolUseResult.Allow());
    }

    private Task<PreToolUseResult> EnforceBlockedCommandsAsync(PreToolUseContext context)
    {
        const string bashTool = "bash";
        if (!context.ToolName.Equals(bashTool, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(PreToolUseResult.Allow());

        if (context.Arguments.TryGetValue("command", out var cmdObj) && cmdObj is string command)
        {
            var blocked = _policy!.BlockedCommands!;
            foreach (var blockedCmd in blocked)
            {
                if (command.Contains(blockedCmd, StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(
                        PreToolUseResult.Block($"Command '{blockedCmd}' is blocked by policy."));
            }
        }

        return Task.FromResult(PreToolUseResult.Allow());
    }

    private Task<PreToolUseResult> EnforceMaxAskUserAsync(PreToolUseContext context)
    {
        const string askUserTool = "ask_user";
        if (!context.ToolName.Equals(askUserTool, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(PreToolUseResult.Allow());

        var count = _askUserCounts.AddOrUpdate(context.SessionId, 1, (_, c) => c + 1);
        var max = _policy!.MaxAskUserPerSession!.Value;

        return count > max
            ? Task.FromResult(PreToolUseResult.Block(
                $"ask_user limit of {max} per session has been reached."))
            : Task.FromResult(PreToolUseResult.Allow());
    }
}
