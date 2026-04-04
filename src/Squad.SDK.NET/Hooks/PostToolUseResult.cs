namespace Squad.SDK.NET.Hooks;

public sealed record PostToolUseResult
{
    public bool Success { get; init; } = true;
    public string? Message { get; init; }
    public string? ScrubbedResult { get; init; }

    public static PostToolUseResult Ok() => new();
    public static PostToolUseResult Fail(string message) => new() { Success = false, Message = message };
}
