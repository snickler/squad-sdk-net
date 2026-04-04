using Squad.SDK.NET.Hooks;

namespace Squad.SDK.NET.Abstractions;

public interface IHookPipeline
{
    void AddPreToolHook(Func<PreToolUseContext, Task<PreToolUseResult>> hook);
    void AddPostToolHook(Func<PostToolUseContext, Task<PostToolUseResult>> hook);
    Task<PreToolUseResult> RunPreToolHooksAsync(PreToolUseContext context, CancellationToken cancellationToken = default);
    Task<PostToolUseResult> RunPostToolHooksAsync(PostToolUseContext context, CancellationToken cancellationToken = default);
}
