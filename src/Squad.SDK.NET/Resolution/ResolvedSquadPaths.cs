namespace Squad.SDK.NET.Resolution;

/// <summary>
/// Contains the resolved directory paths for a squad.
/// </summary>
public sealed record ResolvedSquadPaths
{
    /// <summary>Gets the resolution mode (project, personal, or global).</summary>
    public required SquadMode Mode { get; init; }
    /// <summary>Gets the project-level squad directory path.</summary>
    public required string ProjectDir { get; init; }
    /// <summary>Gets the optional team directory path.</summary>
    public string? TeamDir { get; init; }
    /// <summary>Gets the optional personal squad directory path.</summary>
    public string? PersonalDir { get; init; }
    /// <summary>Gets the resolved squad name.</summary>
    public string? Name { get; init; }
    /// <summary>Gets a value indicating whether this is a legacy squad layout.</summary>
    public bool IsLegacy { get; init; }
}

/// <summary>Indicates how the squad was resolved.</summary>
public enum SquadMode
{
    /// <summary>Project-level squad resolution.</summary>
    Project,

    /// <summary>Personal-level squad resolution.</summary>
    Personal,

    /// <summary>Global-level squad resolution.</summary>
    Global
}

/// <summary>
/// Configuration stored in a squad directory.
/// </summary>
public sealed record SquadDirConfig
{
    /// <summary>Gets the configuration format version.</summary>
    public string Version { get; init; } = "1.0";
    /// <summary>Gets the optional team root directory.</summary>
    public string? TeamRoot { get; init; }
    /// <summary>Gets the optional project key.</summary>
    public string? ProjectKey { get; init; }
    /// <summary>Gets a value indicating whether skill extraction is disabled.</summary>
    public bool ExtractionDisabled { get; init; }
}
