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
}
