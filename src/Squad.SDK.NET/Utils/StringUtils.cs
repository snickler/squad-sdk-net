using System.Text.RegularExpressions;

namespace Squad.SDK.NET.Utils;

/// <summary>
/// Common string manipulation utilities for the Squad SDK.
/// </summary>
public static partial class StringUtils
{
    /// <summary>Normalizes line endings to <c>\n</c> (Unix-style).</summary>
    /// <param name="input">The input string.</param>
    /// <returns>The string with all line endings normalized to <c>\n</c>.</returns>
    public static string NormalizeEol(string input)
    {
        return input.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    /// <summary>Converts a string to a URL-friendly slug (lowercase, hyphen-separated).</summary>
    /// <param name="input">The input string.</param>
    /// <returns>A slugified version of the input, or <see cref="string.Empty"/> if the input is null or whitespace.</returns>
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

    /// <summary>Produces a filesystem-safe timestamp string in the format <c>yyyy-MM-dd-HHmmss</c>.</summary>
    /// <param name="timestamp">Optional timestamp; defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
    /// <returns>A formatted timestamp string.</returns>
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
