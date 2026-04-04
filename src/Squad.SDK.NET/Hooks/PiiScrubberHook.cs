using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Squad.SDK.NET.Hooks;

public sealed partial class PiiScrubberHook
{
    private readonly ILogger<PiiScrubberHook> _logger;

    public PiiScrubberHook(ILogger<PiiScrubberHook> logger)
    {
        _logger = logger;
    }

    public Func<PostToolUseContext, Task<PostToolUseResult>> CreateHook()
    {
        return context =>
        {
            if (context.Result is string resultStr)
            {
                var scrubbed = ScrubPii(resultStr);
                if (scrubbed != resultStr)
                {
                    _logger.LogInformation("Scrubbed PII from tool '{Tool}' result for agent '{Agent}'",
                        context.ToolName, context.AgentName);
                    return Task.FromResult(new PostToolUseResult
                    {
                        Success = true,
                        Message = "PII scrubbed from result",
                        ScrubbedResult = scrubbed
                    });
                }
            }
            return Task.FromResult(PostToolUseResult.Ok());
        };
    }

    public static string ScrubPii(string input)
    {
        // Scrub email addresses
        var result = EmailRegex().Replace(input, "[EMAIL REDACTED]");
        // Scrub phone numbers (US format)
        result = PhoneRegex().Replace(result, "[PHONE REDACTED]");
        // Scrub SSN-like patterns
        result = SsnRegex().Replace(result, "[SSN REDACTED]");
        return result;
    }

    [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex SsnRegex();
}
