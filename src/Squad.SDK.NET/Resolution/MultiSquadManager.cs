using Microsoft.Extensions.Logging;

namespace Squad.SDK.NET.Resolution;

/// <summary>
/// Manages multiple named squads stored under the personal squad directory.
/// </summary>
public sealed class MultiSquadManager
{
    private readonly ILogger<MultiSquadManager> _logger;

    // Explicit cross-platform blocklist — Linux allows chars that Windows forbids,
    // but squad names must be portable across all platforms.
    private static readonly char[] s_portableInvalidChars =
        Path.GetInvalidFileNameChars()
            .Union(['<', '>', ':', '"', '/', '\\', '|', '?', '*'])
            .Distinct()
            .ToArray();

    /// <summary>
    /// Initializes a new <see cref="MultiSquadManager"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public MultiSquadManager(ILogger<MultiSquadManager> logger)
    {
        _logger = logger;
    }

    /// <summary>Lists the names of all squads in the personal squad directory.</summary>
    /// <returns>A sorted read-only list of squad names.</returns>
    public IReadOnlyList<string> ListSquads()
    {
        var personalDir = SquadResolver.ResolvePersonalSquadDir();
        if (personalDir is null || !Directory.Exists(personalDir))
            return Array.Empty<string>();

        return Directory.GetDirectories(personalDir)
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Select(name => name!)
            .Order()
            .ToList()
            .AsReadOnly();
    }

    /// <summary>Creates a new squad directory with the given name.</summary>
    /// <param name="name">The squad name; must be a valid directory name without path separators.</param>
    /// <returns>The full path to the created squad directory.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a squad with the given name already exists.</exception>
    public string CreateSquad(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Squad name is required.", nameof(name));

        if (name.Contains(Path.DirectorySeparatorChar) || name.Contains(Path.AltDirectorySeparatorChar)
            || name.Contains('\\') || name.Contains('/') || name.Contains(".."))
            throw new ArgumentException("Squad name must not contain path separators or '..'.", nameof(name));

        if (name.Any(c => s_portableInvalidChars.Contains(c)))
            throw new ArgumentException("Squad name contains invalid filename characters.", nameof(name));

        var personalDir = SquadResolver.EnsurePersonalSquadDir();
        var squadDir = Path.Combine(personalDir, name);

        var fullSquadDir = Path.GetFullPath(squadDir);
        var fullPersonalDir = Path.GetFullPath(personalDir);
        if (!fullSquadDir.StartsWith(fullPersonalDir, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Squad name resolves outside the personal squad directory.", nameof(name));

        if (Directory.Exists(squadDir))
            throw new InvalidOperationException($"Squad '{name}' already exists.");

        Directory.CreateDirectory(squadDir);
        _logger.LogInformation("Created squad '{Name}' at {Path}", name, squadDir);
        return squadDir;
    }

    /// <summary>Deletes an existing squad directory and all its contents.</summary>
    /// <param name="name">The squad name to delete.</param>
    /// <exception cref="ArgumentException">Thrown when the name is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the squad does not exist or the personal directory cannot be determined.</exception>
    public void DeleteSquad(string name)
    {
        var personalDir = SquadResolver.ResolvePersonalSquadDir();
        if (personalDir is null)
            throw new InvalidOperationException("Cannot determine personal squad directory.");

        if (name.Contains(Path.DirectorySeparatorChar) || name.Contains(Path.AltDirectorySeparatorChar)
            || name.Contains('\\') || name.Contains('/') || name.Contains(".."))
            throw new ArgumentException("Squad name must not contain path separators or '..'.", nameof(name));

        if (name.Any(c => s_portableInvalidChars.Contains(c)))
            throw new ArgumentException("Squad name contains invalid filename characters.", nameof(name));

        var squadDir = Path.Combine(personalDir, name);

        var fullSquadDir = Path.GetFullPath(squadDir);
        var fullPersonalDir = Path.GetFullPath(personalDir);
        if (!fullSquadDir.StartsWith(fullPersonalDir, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Squad name resolves outside the personal squad directory.", nameof(name));

        if (!Directory.Exists(squadDir))
            throw new InvalidOperationException($"Squad '{name}' does not exist.");

        Directory.Delete(squadDir, recursive: true);
        _logger.LogInformation("Deleted squad '{Name}'", name);
    }

    /// <summary>Resolves the directory path for a squad by name, or the current project squad if <paramref name="name"/> is <see langword="null"/>.</summary>
    /// <param name="name">Optional squad name; when <see langword="null"/>, resolves the current project squad.</param>
    /// <returns>The squad directory path, or <see langword="null"/> if not found.</returns>
    public string? ResolveSquadPath(string? name = null)
    {
        if (name is null)
        {
            var resolved = SquadResolver.ResolveSquad();
            return resolved?.ProjectDir;
        }

        var personalDir = SquadResolver.ResolvePersonalSquadDir();
        if (personalDir is null) return null;

        var squadDir = Path.Combine(personalDir, name);
        return Directory.Exists(squadDir) ? squadDir : null;
    }
}
