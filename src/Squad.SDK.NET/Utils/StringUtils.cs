using System.Text.RegularExpressions;

namespace Squad.SDK.NET.Utils;

public static partial class StringUtils
{
    public static string NormalizeEol(string input)
    {
        return input.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    public static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var normalized = input.ToLowerInvariant().Trim();
        // Replace non-alphanumeric chars with hyphens
        normalized = NonAlphanumericRegex().Replace(normalized, "-");
        // Collapse multiple hyphens
        normalized = MultipleHyphensRegex().Replace(normalized, "-");
        // Trim leading/trailing hyphens
        return normalized.Trim('-');
    }

    public static string SafeTimestamp(DateTimeOffset? timestamp = null)
    {
        var ts = timestamp ?? DateTimeOffset.UtcNow;
        return ts.ToString("yyyy-MM-dd-HHmmss");
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultipleHyphensRegex();
}
