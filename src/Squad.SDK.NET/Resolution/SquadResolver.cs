using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

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
    /// <summary>Name of the scratch subdirectory inside a squad root.</summary>
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

    /// <summary>
    /// Resolves the scratch directory for temporary files inside a squad root.
    /// Returns <c>{squadRoot}/.scratch/</c> — the canonical location for ephemeral files
    /// (prompt files, intermediate processing artifacts, commit message drafts, etc.).
    /// </summary>
    /// <param name="squadRoot">Absolute path to the <c>.squad/</c> directory.</param>
    /// <param name="create">Whether to create the directory if it does not exist (default: <see langword="true"/>).</param>
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
    /// Writes <paramref name="content"/> to the file if provided; otherwise the caller is responsible for writing.
    /// The caller is also responsible for deleting the file when done.
    /// </summary>
    /// <param name="squadRoot">Absolute path to the <c>.squad/</c> directory.</param>
    /// <param name="prefix">Filename prefix (e.g. <c>"fleet-prompt"</c>).</param>
    /// <param name="ext">File extension including the dot (e.g. <c>".txt"</c>). Defaults to <c>".tmp"</c>.</param>
    /// <param name="content">Optional content to write immediately.</param>
    /// <returns>Absolute path to the temporary file.</returns>
    public static string ScratchFile(string squadRoot, string prefix, string ext = ".tmp", string? content = null)
    {
        // Sanitize prefix to prevent path traversal — strip directory components
        var safePrefix = Path.GetFileName(prefix);
        var safeExt = ext.Replace('/', '_').Replace('\\', '_');

        var dir = ScratchDir(squadRoot);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var rand = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();

        var filename = $"{safePrefix}-{timestamp}-{rand}{safeExt}";
        var filePath = Path.Combine(dir, filename);

        if (content is not null)
            File.WriteAllText(filePath, content, Encoding.UTF8);

        return filePath;
    }

    /// <summary>
    /// Derives a stable project key from a project directory path.
    /// Takes the base name of the path, lowercases it, and replaces unsafe characters with dashes.
    /// Returns <c>"unknown-project"</c> if the base name is empty.
    /// </summary>
    /// <param name="projectDir">Absolute path to the project root.</param>
    /// <returns>A sanitized, lowercase project key suitable for use as a directory name.</returns>
    public static string DeriveProjectKey(string projectDir)
    {
        var normalized = projectDir.Replace('\\', '/');
        var baseName = Path.GetFileName(normalized.TrimEnd('/'));
        if (string.IsNullOrEmpty(baseName))
            return "unknown-project";

        // Lowercase and replace any character that is not alphanumeric, dot, underscore, or dash
        var sb = new StringBuilder(baseName.Length);
        foreach (var ch in baseName.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch) || ch == '.' || ch == '_' || ch == '-')
                sb.Append(ch);
            else
                sb.Append('-');
        }

        var sanitized = sb.ToString().Trim('-');
        return string.IsNullOrEmpty(sanitized) ? "unknown-project" : sanitized;
    }

    /// <summary>
    /// Resolves the external state directory for a project.
    /// Returns <c>{personalDir}/projects/{sanitizedKey}/</c> where <c>personalDir</c> is
    /// the platform-specific user config directory (e.g. <c>%APPDATA%/squad</c> on Windows,
    /// <c>~/.config/squad</c> on Linux/macOS).
    /// </summary>
    /// <param name="projectKey">The project key (from <see cref="DeriveProjectKey"/> or user-supplied).</param>
    /// <param name="create">Whether to create the directory if it does not exist (default: <see langword="true"/>).</param>
    /// <returns>Absolute path to the project's external state directory.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="projectKey"/> is empty or contains path traversal sequences.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the personal squad directory cannot be determined.</exception>
    public static string ResolveExternalStateDir(string projectKey, bool create = true)
    {
        if (string.IsNullOrEmpty(projectKey) || projectKey.Contains(".."))
            throw new ArgumentException("Invalid project key.", nameof(projectKey));

        // Sanitize: replace path separators and unsafe chars with dashes
        var sb = new StringBuilder(projectKey.Length);
        foreach (var ch in projectKey)
        {
            if (char.IsLetterOrDigit(ch) || ch == '.' || ch == '_' || ch == '-')
                sb.Append(ch);
            else
                sb.Append('-');
        }

        var sanitized = sb.ToString().Trim('-');
        if (string.IsNullOrEmpty(sanitized))
            throw new ArgumentException("Invalid project key.", nameof(projectKey));

        var personalDir = ResolvePersonalSquadDir()
            ?? throw new InvalidOperationException("Cannot determine personal squad directory.");

        var projectsDir = Path.Combine(personalDir, "projects", sanitized);

        if (create && !Directory.Exists(projectsDir))
            Directory.CreateDirectory(projectsDir);

        return projectsDir;
    }
}
