using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Squad.SDK.NET.Resolution;

/// <summary>
/// Resolves squad directory locations by walking up the directory tree and checking platform-specific paths.
/// </summary>
public static partial class SquadResolver
{
    /// <summary>Name of the squad directory marker.</summary>
    public const string SquadDirName = ".squad";
    /// <summary>Name of the squad configuration file.</summary>
    public const string ConfigFileName = "squad.json";

    /// <summary>Name of the scratch directory inside a squad directory.</summary>
    public const string ScratchDirName = ".scratch";

    /// <summary>Resolves a squad by walking up the directory tree from <paramref name="startDir"/>.</summary>
    /// <param name="startDir">Directory to start searching from; defaults to the current directory.</param>
    /// <returns>The resolved paths, or <see langword="null"/> if no squad directory is found.</returns>
    public static ResolvedSquadPaths? ResolveSquad(string? startDir = null)
    {
        var searchDir = startDir ?? Directory.GetCurrentDirectory();

        // Walk up the directory tree looking for .squad/
        var current = new DirectoryInfo(searchDir);
        while (current is not null)
        {
            var squadDir = Path.Combine(current.FullName, SquadDirName);
            if (Directory.Exists(squadDir))
            {
                return new ResolvedSquadPaths
                {
                    Mode = SquadMode.Project,
                    ProjectDir = squadDir,
                    PersonalDir = ResolvePersonalSquadDir(),
                    Name = current.Name
                };
            }
            current = current.Parent;
        }

        // Fall back to personal squad dir
        var personalDir = ResolvePersonalSquadDir();
        if (personalDir is not null && Directory.Exists(personalDir))
        {
            return new ResolvedSquadPaths
            {
                Mode = SquadMode.Personal,
                ProjectDir = personalDir,
                PersonalDir = personalDir,
                Name = "personal"
            };
        }

        return null;
    }

    /// <summary>Resolves the platform-specific personal squad directory path.</summary>
    /// <returns>The directory path, or <see langword="null"/> if it cannot be determined.</returns>
    public static string? ResolvePersonalSquadDir()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return string.IsNullOrEmpty(appData) ? null : Path.Combine(appData, "squad");
        }

        // macOS/Linux: XDG_CONFIG_HOME or ~/.config/squad
        var xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrEmpty(xdgConfig))
            return Path.Combine(xdgConfig, "squad");

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrEmpty(home) ? null : Path.Combine(home, ".config", "squad");
    }

    /// <summary>Resolves the global squad configuration path (system-wide).</summary>
    /// <returns>The directory path, or <see langword="null"/> if it cannot be determined.</returns>
    public static string? ResolveGlobalSquadPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return string.IsNullOrEmpty(programData) ? null : Path.Combine(programData, "squad");
        }

        return "/etc/squad";
    }

    /// <summary>Ensures the personal squad directory exists, creating it if necessary.</summary>
    /// <returns>The full path to the personal squad directory.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the personal squad directory cannot be determined.</exception>
    public static string EnsurePersonalSquadDir()
    {
        var dir = ResolvePersonalSquadDir() ?? throw new InvalidOperationException("Cannot determine personal squad directory.");
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>Determines whether the given directory is inside a git worktree.</summary>
    /// <param name="dir">Directory to check; defaults to the current directory.</param>
    /// <returns><see langword="true"/> if a <c>.git</c> file (not directory) exists, indicating a worktree.</returns>
    public static bool IsInsideWorktree(string? dir = null)
    {
        var searchDir = dir ?? Directory.GetCurrentDirectory();
        // Check for .git file (not directory) which indicates a worktree
        var gitPath = Path.Combine(searchDir, ".git");
        return File.Exists(gitPath) && !Directory.Exists(gitPath);
    }

    // =========================================================================
    // Scratch directory utilities
    // =========================================================================

    /// <summary>
    /// Resolves the scratch directory for temporary files inside a squad root.
    /// Returns <c>{squadRoot}/.scratch/</c> — the canonical location for ephemeral
    /// files created during agent operations (prompt files, intermediate artifacts,
    /// commit message drafts, etc.).
    /// </summary>
    /// <param name="squadRoot">Absolute path to the <c>.squad/</c> directory.</param>
    /// <param name="create">
    /// When <see langword="true"/> (default), the directory is created if it does not exist.
    /// </param>
    /// <returns>Absolute path to the scratch directory.</returns>
    public static string ScratchDir(string squadRoot, bool create = true)
    {
        var dir = Path.Combine(squadRoot, ScratchDirName);
        if (create && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Returns a unique file path inside the scratch directory.
    /// Writes content to the file if <paramref name="content"/> is provided; otherwise the caller
    /// is responsible for writing to it. The caller is also responsible for cleanup.
    /// </summary>
    /// <param name="squadRoot">Absolute path to the <c>.squad/</c> directory.</param>
    /// <param name="prefix">Filename prefix (e.g. <c>"fleet-prompt"</c>).</param>
    /// <param name="ext">File extension including dot (e.g. <c>".txt"</c>). Defaults to <c>".tmp"</c>.</param>
    /// <param name="content">Optional content to write immediately.</param>
    /// <returns>Absolute path to the temp file.</returns>
    public static string ScratchFile(string squadRoot, string prefix, string ext = ".tmp", string? content = null)
    {
        // Sanitize prefix to prevent path traversal — strip directory components
        var safePrefix = Path.GetFileName(prefix);
        var safeExt = ext.Replace('/', '_').Replace('\\', '_');

        var dir = ScratchDir(squadRoot);

        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var rand = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();

        var filename = $"{safePrefix}-{nowMs}-{rand}{safeExt}";
        var filePath = Path.Combine(dir, filename);

        if (content is not null)
            File.WriteAllText(filePath, content);

        return filePath;
    }

    // =========================================================================
    // Path validation helpers
    // =========================================================================

    /// <summary>
    /// Validates that <paramref name="filePath"/> is within the given <paramref name="squadRoot"/> directory
    /// or the system temp directory.
    /// </summary>
    /// <param name="filePath">Absolute path to validate.</param>
    /// <param name="squadRoot">Absolute path to the squad root directory.</param>
    /// <returns>The resolved absolute path if it is safe.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="filePath"/> is outside the squad root and system temp directory.
    /// </exception>
    public static string EnsureSquadPath(string filePath, string squadRoot)
    {
        var resolved = Path.GetFullPath(filePath);
        var resolvedSquad = Path.GetFullPath(squadRoot);
        var resolvedTmp = Path.GetFullPath(Path.GetTempPath());

        if (IsUnderDirectory(resolved, resolvedSquad) || IsUnderDirectory(resolved, resolvedTmp))
            return resolved;

        throw new ArgumentException(
            $"Path \"{resolved}\" is outside the .squad/ directory (\"{resolvedSquad}\"). " +
            "All squad scratch/temp/state files must be written inside .squad/ or the system temp directory.",
            nameof(filePath));
    }

    /// <summary>
    /// Validates that <paramref name="filePath"/> is within either <paramref name="projectDir"/> or
    /// <paramref name="teamDir"/> (or the system temp directory). For use in dual-root / remote mode.
    /// </summary>
    /// <param name="filePath">Absolute path to validate.</param>
    /// <param name="projectDir">Absolute path to the project-local <c>.squad/</c> directory.</param>
    /// <param name="teamDir">Absolute path to the team identity directory.</param>
    /// <returns>The resolved absolute path if it is safe.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="filePath"/> is outside both roots and the system temp directory.
    /// </exception>
    public static string EnsureSquadPathDual(string filePath, string projectDir, string teamDir)
    {
        var resolved = Path.GetFullPath(filePath);
        var resolvedProject = Path.GetFullPath(projectDir);
        var resolvedTeam = Path.GetFullPath(teamDir);
        var resolvedTmp = Path.GetFullPath(Path.GetTempPath());

        if (IsUnderDirectory(resolved, resolvedProject) ||
            IsUnderDirectory(resolved, resolvedTeam) ||
            IsUnderDirectory(resolved, resolvedTmp))
            return resolved;

        throw new ArgumentException(
            $"Path \"{resolved}\" is outside both squad roots (\"{resolvedProject}\", \"{resolvedTeam}\"). " +
            "All squad scratch/temp/state files must be written inside a squad directory or the system temp directory.",
            nameof(filePath));
    }

    /// <summary>
    /// Validates that <paramref name="filePath"/> is within one of three allowed directories:
    /// <paramref name="projectDir"/>, <paramref name="teamDir"/>, <paramref name="personalDir"/>,
    /// or the system temp directory.
    /// </summary>
    /// <param name="filePath">Absolute path to validate.</param>
    /// <param name="projectDir">Absolute path to the project-local <c>.squad/</c> directory.</param>
    /// <param name="teamDir">Absolute path to the team identity directory.</param>
    /// <param name="personalDir">Absolute path to the personal squad directory, or <see langword="null"/>.</param>
    /// <returns>The resolved absolute path if it is safe.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="filePath"/> is outside all allowed directories.
    /// </exception>
    public static string EnsureSquadPathTriple(string filePath, string projectDir, string teamDir, string? personalDir)
    {
        var resolved = Path.GetFullPath(filePath);
        var tmpDir = Path.GetTempPath();
        var allowed = new[] { projectDir, teamDir, personalDir, tmpDir }
            .Where(d => d is not null)
            .Select(d => Path.GetFullPath(d!))
            .ToArray();

        if (allowed.Any(dir => IsUnderDirectory(resolved, dir)))
            return resolved;

        throw new ArgumentException(
            $"Path \"{resolved}\" is outside all allowed directories: {string.Join(", ", allowed)}",
            nameof(filePath));
    }

    /// <summary>
    /// Validates that <paramref name="filePath"/> is within the resolved squad paths.
    /// Convenience wrapper around <see cref="EnsureSquadPathDual"/>.
    /// </summary>
    /// <param name="filePath">Absolute path to validate.</param>
    /// <param name="paths">The resolved squad paths containing projectDir and teamDir.</param>
    /// <returns>The resolved absolute path if it is safe.</returns>
    public static string EnsureSquadPathResolved(string filePath, ResolvedSquadPaths paths)
    {
        var teamDir = paths.TeamDir ?? paths.ProjectDir;
        return EnsureSquadPathDual(filePath, paths.ProjectDir, teamDir);
    }

    // =========================================================================
    // External state storage
    // =========================================================================

    /// <summary>
    /// Derives a stable project key from a project directory path.
    /// Takes the basename of the path, lowercases it, and replaces unsafe characters
    /// with dashes. Returns <c>"unknown-project"</c> if the basename is empty.
    /// </summary>
    /// <param name="projectDir">Absolute path to the project root.</param>
    /// <returns>A sanitized, lowercase project key suitable for use as a directory name.</returns>
    public static string DeriveProjectKey(string projectDir)
    {
        // Normalize Windows backslashes then get basename
        var normalized = projectDir.Replace('\\', '/');
        var baseName = Path.GetFileName(normalized.TrimEnd('/'));

        if (string.IsNullOrEmpty(baseName))
            return "unknown-project";

        var sanitized = baseName
            .ToLowerInvariant()
            .Replace(' ', '-');

        // Replace characters that are not alphanumeric, dot, underscore, or dash
        sanitized = UnsafeProjectKeyChars().Replace(sanitized, "-");
        // Trim leading/trailing dashes
        sanitized = sanitized.Trim('-');

        return string.IsNullOrEmpty(sanitized) ? "unknown-project" : sanitized;
    }

    /// <summary>
    /// Resolves the external state directory for a project.
    /// Returns <c>{personalDir}/projects/{sanitizedKey}/</c> where <c>personalDir</c> is the
    /// platform-specific personal config directory.
    /// </summary>
    /// <param name="projectKey">The project key (from <see cref="DeriveProjectKey"/> or user-supplied).</param>
    /// <param name="create">Whether to create the directory if it doesn't exist (default: <see langword="true"/>).</param>
    /// <returns>Absolute path to the project's external state directory.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="projectKey"/> is empty or contains path traversal sequences.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the personal squad directory cannot be determined.</exception>
    public static string ResolveExternalStateDir(string projectKey, bool create = true)
    {
        if (string.IsNullOrEmpty(projectKey) || projectKey.Contains(".."))
            throw new ArgumentException("Invalid project key", nameof(projectKey));

        // Sanitize: replace path separators and unsafe chars with dashes, trim edges
        var sanitized = projectKey
            .Replace('/', '-')
            .Replace('\\', '-');
        sanitized = UnsafeProjectKeyChars().Replace(sanitized, "-").Trim('-');

        if (string.IsNullOrEmpty(sanitized))
            throw new ArgumentException("Invalid project key", nameof(projectKey));

        var personalDir = ResolvePersonalSquadDir()
            ?? throw new InvalidOperationException("Cannot determine personal squad directory.");

        var projectsDir = Path.Combine(personalDir, "projects", sanitized);

        if (create && !Directory.Exists(projectsDir))
            Directory.CreateDirectory(projectsDir);

        return projectsDir;
    }

    // =========================================================================
    // Private helpers
    // =========================================================================

    private static bool IsUnderDirectory(string path, string directory)
    {
        // Normalize by removing any trailing directory separator from the directory reference
        var dir = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return path == dir ||
               path.StartsWith(dir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith(dir + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"[^a-z0-9._\-]")]
    private static partial Regex UnsafeProjectKeyChars();
}
