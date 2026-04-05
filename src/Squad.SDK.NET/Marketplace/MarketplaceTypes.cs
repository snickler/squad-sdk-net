namespace Squad.SDK.NET.Marketplace;

/// <summary>
/// Describes a squad package for marketplace distribution.
/// </summary>
public sealed record MarketplaceManifest
{
    /// <summary>Gets the package name.</summary>
    public required string Name { get; init; }
    /// <summary>Gets the package version (SemVer).</summary>
    public required string Version { get; init; }
    /// <summary>Gets an optional package description.</summary>
    public string? Description { get; init; }
    /// <summary>Gets the optional author name.</summary>
    public string? Author { get; init; }
    /// <summary>Gets the optional license identifier (e.g., "MIT").</summary>
    public string? License { get; init; }
    /// <summary>Gets the optional homepage URL.</summary>
    public string? Homepage { get; init; }
    /// <summary>Gets the optional source repository URL.</summary>
    public string? Repository { get; init; }
    /// <summary>Gets the marketplace category.</summary>
    public ManifestCategory Category { get; init; } = ManifestCategory.General;
    /// <summary>Gets the tags for search and discovery.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
    /// <summary>Gets the capabilities declared by this package.</summary>
    public IReadOnlyList<ManifestCapability> Capabilities { get; init; } = [];
    /// <summary>Gets optional free-form metadata.</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>Categories for marketplace manifest classification.</summary>
public enum ManifestCategory
{
    /// <summary>General-purpose package.</summary>
    General,
    /// <summary>Development tools and utilities.</summary>
    Development,
    /// <summary>Testing and QA tools.</summary>
    Testing,
    /// <summary>Documentation generation or management.</summary>
    Documentation,
    /// <summary>Security scanning and compliance.</summary>
    Security,
    /// <summary>DevOps, CI/CD, and infrastructure.</summary>
    DevOps,
    /// <summary>Data processing and analytics.</summary>
    Data,
    /// <summary>Artificial intelligence and machine learning.</summary>
    AI
}

/// <summary>
/// Describes a single capability offered by a marketplace package.
/// </summary>
public sealed record ManifestCapability
{
    /// <summary>Gets the capability name.</summary>
    public required string Name { get; init; }
    /// <summary>Gets an optional capability description.</summary>
    public string? Description { get; init; }
    /// <summary>Gets a value indicating whether this capability is required.</summary>
    public bool Required { get; init; }
}
