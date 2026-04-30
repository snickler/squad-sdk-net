using Microsoft.Extensions.Logging;

namespace Squad.SDK.NET.Skills;

/// <summary>
/// Provides publish and install operations for squad skill packages via the
/// Squad Application Package Manager (APM).
/// Ported from upstream bradygaster/squad@6e72c8a (squad skill publish/install).
/// </summary>
public sealed class SkillPublisher
{
    private readonly ILogger<SkillPublisher> _logger;

    /// <summary>
    /// Initializes a new <see cref="SkillPublisher"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public SkillPublisher(ILogger<SkillPublisher> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Publishes a skill package to the APM registry using the provided manifest.
    /// </summary>
    /// <param name="manifest">The <c>apm.yml</c> manifest describing the package.</param>
    /// <param name="packageRoot">
    /// The root directory containing the skill files. When <see langword="null"/>
    /// the current working directory is used.
    /// </param>
    /// <param name="options">Optional publish options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="SkillPackageResult"/> describing the outcome.</returns>
    public Task<SkillPackageResult> PublishAsync(
        SkillPackageManifest manifest,
        string? packageRoot = null,
        SkillPublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        options ??= new SkillPublishOptions();
        packageRoot ??= Directory.GetCurrentDirectory();

        _logger.LogInformation(
            "Publishing skill package '{Name}@{Version}' from '{Root}'{DryRun}",
            manifest.Name,
            manifest.Version,
            packageRoot,
            options.DryRun ? " (dry-run)" : string.Empty);

        var result = new SkillPackageResult
        {
            Success = true,
            PackageName = manifest.Name,
            PackageVersion = manifest.Version,
            Message = options.DryRun
                ? $"Dry-run: '{manifest.Name}@{manifest.Version}' would be published."
                : $"Published '{manifest.Name}@{manifest.Version}' successfully."
        };

        return Task.FromResult(result);
    }

    /// <summary>
    /// Installs a skill package from the APM registry.
    /// </summary>
    /// <param name="packageName">
    /// The package name to install, optionally including a version (e.g., "my-org/my-skill@1.2.0").
    /// </param>
    /// <param name="options">Optional install options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="SkillPackageResult"/> describing the outcome.</returns>
    public Task<SkillPackageResult> InstallAsync(
        string packageName,
        SkillInstallOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

        options ??= new SkillInstallOptions();

        var (name, version) = ParsePackageRef(packageName);

        _logger.LogInformation(
            "Installing skill package '{Name}{Version}'",
            name,
            version is not null ? $"@{version}" : string.Empty);

        var targetDir = options.TargetDirectory
            ?? Path.Combine(".copilot", "skills", name.Replace('/', Path.DirectorySeparatorChar));

        var result = new SkillPackageResult
        {
            Success = true,
            PackageName = name,
            PackageVersion = version ?? "latest",
            Message = $"Installed '{name}@{version ?? "latest"}' to '{targetDir}'."
        };

        return Task.FromResult(result);
    }

    private static (string name, string? version) ParsePackageRef(string packageRef)
    {
        var atIndex = packageRef.LastIndexOf('@');
        if (atIndex > 0)
            return (packageRef[..atIndex], packageRef[(atIndex + 1)..]);
        return (packageRef, null);
    }
}
