namespace Squad.SDK.NET.Skills;

/// <summary>
/// Represents the <c>apm.yml</c> manifest for a squad skill package, enabling
/// publish and install operations via the Squad Application Package Manager (APM).
/// Ported from upstream bradygaster/squad@6e72c8a.
/// </summary>
public sealed record SkillPackageManifest
{
    /// <summary>Gets the unique package name (e.g., "my-org/my-skill").</summary>
    public required string Name { get; init; }

    /// <summary>Gets the package version in SemVer format (e.g., "1.0.0").</summary>
    public required string Version { get; init; }

    /// <summary>Gets an optional human-readable description of the skill package.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the optional author or organization name.</summary>
    public string? Author { get; init; }

    /// <summary>Gets the optional SPDX license identifier (e.g., "MIT").</summary>
    public string? License { get; init; }

    /// <summary>Gets the optional URL to the package homepage or repository.</summary>
    public string? Repository { get; init; }

    /// <summary>
    /// Gets the relative paths to SKILL.md files included in this package.
    /// When empty the publisher will auto-discover all SKILL.md files under the package root.
    /// </summary>
    public IReadOnlyList<string> Skills { get; init; } = [];

    /// <summary>Gets optional free-form metadata key-value pairs.</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>Options that control how a skill package is published to the APM registry.</summary>
public sealed record SkillPublishOptions
{
    /// <summary>Gets the registry URL to publish to. Defaults to the public APM registry.</summary>
    public string? RegistryUrl { get; init; }

    /// <summary>Gets a value indicating whether to perform a dry run without uploading.</summary>
    public bool DryRun { get; init; }

    /// <summary>Gets a value indicating whether to overwrite an existing version.</summary>
    public bool Force { get; init; }
}

/// <summary>Options that control how a skill package is installed from the APM registry.</summary>
public sealed record SkillInstallOptions
{
    /// <summary>Gets the registry URL to install from. Defaults to the public APM registry.</summary>
    public string? RegistryUrl { get; init; }

    /// <summary>
    /// Gets the target directory where the skill files will be written.
    /// Defaults to <c>.copilot/skills/&lt;package-name&gt;</c>.
    /// </summary>
    public string? TargetDirectory { get; init; }

    /// <summary>Gets a value indicating whether to overwrite existing skill files.</summary>
    public bool Overwrite { get; init; }
}

/// <summary>Represents the result of a skill package publish or install operation.</summary>
public sealed record SkillPackageResult
{
    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public required bool Success { get; init; }

    /// <summary>Gets the package name that was published or installed.</summary>
    public required string PackageName { get; init; }

    /// <summary>Gets the package version that was published or installed.</summary>
    public required string PackageVersion { get; init; }

    /// <summary>Gets an optional message describing the result or any warnings.</summary>
    public string? Message { get; init; }
}
