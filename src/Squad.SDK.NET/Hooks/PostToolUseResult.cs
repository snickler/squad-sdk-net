namespace Squad.SDK.NET.Hooks;

/// <summary>
/// Represents the outcome of a post-tool-use hook.
/// </summary>
/// <seealso cref="PostToolUseContext"/>
public sealed record PostToolUseResult
{
    /// <summary>Gets a value indicating whether the post-hook processing succeeded.</summary>
    public bool Success { get; init; } = true;

    /// <summary>Gets an optional message describing the result or failure reason.</summary>
    public string? Message { get; init; }

    /// <summary>Gets the scrubbed version of the tool result, if PII or sensitive data was removed.</summary>
    public string? ScrubbedResult { get; init; }

    /// <summary>Creates a successful post-tool-use result.</summary>
    /// <returns>A <see cref="PostToolUseResult"/> with <see cref="Success"/> set to <see langword="true"/>.</returns>
    public static PostToolUseResult Ok() => new();

    /// <summary>Creates a failed post-tool-use result.</summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A <see cref="PostToolUseResult"/> with <see cref="Success"/> set to <see langword="false"/>.</returns>
    public static PostToolUseResult Fail(string message) => new() { Success = false, Message = message };
}
