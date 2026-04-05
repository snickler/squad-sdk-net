namespace Squad.SDK.NET.Sharing;

/// <summary>
/// Represents a squad exported to a portable JSON format.
/// </summary>
public sealed record ExportedSquad
{
    /// <summary>Gets the squad name.</summary>
    public required string Name { get; init; }
    /// <summary>Gets the squad version.</summary>
    public required string Version { get; init; }
    /// <summary>Gets an optional squad description.</summary>
    public string? Description { get; init; }
    /// <summary>Gets the optional author attribution.</summary>
    public string? Author { get; init; }
    /// <summary>Gets the raw JSON representation of the <see cref="Config.SquadConfig"/>.</summary>
    public required string ConfigJson { get; init; }
    /// <summary>Gets the list of exported agents.</summary>
    public IReadOnlyList<ExportedAgent> Agents { get; init; } = [];
    /// <summary>Gets the timestamp when the squad was exported.</summary>
    public DateTimeOffset ExportedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents a single agent within an <see cref="ExportedSquad"/>.
/// </summary>
public sealed record ExportedAgent
{
    /// <summary>Gets the agent name.</summary>
    public required string Name { get; init; }
    /// <summary>Gets the agent role identifier.</summary>
    public required string Role { get; init; }
    /// <summary>Gets the optional agent charter.</summary>
    public string? Charter { get; init; }
    /// <summary>Gets the optional agent prompt.</summary>
    public string? Prompt { get; init; }
}

/// <summary>
/// Result of a squad import operation.
/// </summary>
public sealed record ImportResult
{
    /// <summary>Gets a value indicating whether the import succeeded.</summary>
    public bool Success { get; init; }
    /// <summary>Gets a human-readable status message.</summary>
    public string? Message { get; init; }
    /// <summary>Gets any warnings encountered during import.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
    /// <summary>Gets the file path from which the squad was imported.</summary>
    public string? ImportedPath { get; init; }
}
