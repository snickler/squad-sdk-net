using System.Runtime.InteropServices;

namespace Squad.SDK.NET.Resolution;

/// <summary>
/// Resolves squad directory locations by walking up the directory tree and checking platform-specific paths.
/// </summary>
public static class SquadResolver
{
    /// <summary>Name of the squad directory marker.</summary>
    public const string SquadDirName = ".squad";
    /// <summary>Name of the squad configuration file.</summary>
    public const string ConfigFileName = "squad.json";

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

    /// <summary>
    /// Returns the scratch directory path inside <paramref name="squadRoot"/>, creating it if it does not exist.
    /// </summary>
    /// <param name="squadRoot">Absolute path to the <c>.squad/</c> directory.</param>
    /// <param name="create">Whether to create the directory when it does not exist (default: <see langword="true"/>).</param>
    /// <returns>Absolute path to the <c>.scratch</c> subdirectory.</returns>
    public static string ScratchDir(string squadRoot, bool create = true)
    {
        var dir = Path.Combine(squadRoot, ".scratch");
        if (create && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Returns a unique file path inside the scratch directory.
    /// Writes <paramref name="content"/> to the file when provided; otherwise the file is not created and the
    /// caller is responsible for writing to it.
    /// </summary>
    /// <param name="squadRoot">Absolute path to the <c>.squad/</c> directory.</param>
    /// <param name="prefix">Filename prefix (e.g. <c>"fleet-prompt"</c>).</param>
    /// <param name="ext">File extension including dot (e.g. <c>".txt"</c>). Defaults to <c>".tmp"</c>.</param>
    /// <param name="content">Optional content to write immediately.</param>
    /// <returns>Absolute path to the scratch file.</returns>
    public static string ScratchFile(string squadRoot, string prefix, string ext = ".tmp", string? content = null)
    {
        // Sanitize prefix to prevent path traversal — strip directory components
        var safePrefix = Path.GetFileName(prefix);
        var safeExt = ext.Replace('/', '_').Replace('\\', '_');

        var dir = ScratchDir(squadRoot);

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var rand = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();

        var filename = $"{safePrefix}-{now}-{rand}{safeExt}";
        var filePath = Path.Combine(dir, filename);
        if (content is not null)
            File.WriteAllText(filePath, content);
        return filePath;
    }

    /// <summary>
    /// Derives a stable project key from a project directory path.
    /// Takes the basename of the path, lowercases it, and replaces unsafe characters with dashes.
    /// </summary>
    /// <param name="projectDir">Absolute path to the project root.</param>
    /// <returns>A sanitized, lowercase project key suitable for use as a directory name.</returns>
    public static string DeriveProjectKey(string projectDir)
    {
        var normalized = projectDir.Replace('\\', '/');
        var baseName = Path.GetFileName(normalized.TrimEnd('/'));
        if (string.IsNullOrEmpty(baseName))
            return "unknown-project";

        // Lowercase and replace unsafe chars with dashes
        var sanitized = System.Text.RegularExpressions.Regex.Replace(
            baseName.ToLowerInvariant(),
            @"[^a-z0-9._-]",
            "-");
        sanitized = sanitized.Trim('-');

        return string.IsNullOrEmpty(sanitized) ? "unknown-project" : sanitized;
    }

    /// <summary>
    /// Resolves the external state directory for a project.
    /// The path is <c>{personalDir}/projects/{sanitizedKey}/</c> where <c>personalDir</c>
    /// is the user-level squad config directory (see <see cref="ResolvePersonalSquadDir"/>).
    /// </summary>
    /// <param name="projectKey">The project key (from <see cref="DeriveProjectKey"/> or user-supplied).</param>
    /// <param name="create">Whether to create the directory when it does not exist (default: <see langword="true"/>).</param>
    /// <returns>Absolute path to the project's external state directory.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="projectKey"/> is empty or contains path traversal sequences.
    /// </exception>
    public static string ResolveExternalStateDir(string projectKey, bool create = true)
    {
        if (string.IsNullOrWhiteSpace(projectKey) || projectKey.Contains(".."))
            throw new ArgumentException("Invalid project key.", nameof(projectKey));

        // Sanitize: replace path separators and unsafe chars with dashes
        var sanitized = System.Text.RegularExpressions.Regex.Replace(
            projectKey.Replace('/', '-').Replace('\\', '-'),
            @"[^a-zA-Z0-9._-]",
            "-");
        sanitized = sanitized.Trim('-');

        if (string.IsNullOrEmpty(sanitized))
            throw new ArgumentException("Invalid project key.", nameof(projectKey));

        var baseDir = ResolvePersonalSquadDir()
            ?? throw new InvalidOperationException("Cannot determine personal squad directory.");
        var projectsDir = Path.Combine(baseDir, "projects", sanitized);

        if (create && !Directory.Exists(projectsDir))
            Directory.CreateDirectory(projectsDir);

        return projectsDir;
    }
}
