namespace Squad.SDK.NET.Skills;

/// <summary>
/// Represents a security finding detected in a skill markdown file.
/// </summary>
public sealed record SkillSecurityFinding
{
    /// <summary>Gets the finding category (e.g., <c>"skill-credentials"</c>).</summary>
    public required string Category { get; init; }

    /// <summary>Gets the finding severity (e.g., <c>"error"</c>).</summary>
    public required string Severity { get; init; }

    /// <summary>Gets the human-readable description of the finding.</summary>
    public required string Message { get; init; }

    /// <summary>Gets the repository-relative file path where the finding was detected.</summary>
    public required string File { get; init; }

    /// <summary>Gets the 1-based line number where the finding was detected.</summary>
    public required int Line { get; init; }
}
