using System.Runtime.InteropServices;

namespace Squad.SDK.NET.Resolution;

public static class SquadResolver
{
    public const string SquadDirName = ".squad";
    public const string ConfigFileName = "squad.json";

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

    public static string? ResolveGlobalSquadPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return string.IsNullOrEmpty(programData) ? null : Path.Combine(programData, "squad");
        }

        return "/etc/squad";
    }

    public static string EnsurePersonalSquadDir()
    {
        var dir = ResolvePersonalSquadDir() ?? throw new InvalidOperationException("Cannot determine personal squad directory.");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static bool IsInsideWorktree(string? dir = null)
    {
        var searchDir = dir ?? Directory.GetCurrentDirectory();
        // Check for .git file (not directory) which indicates a worktree
        var gitPath = Path.Combine(searchDir, ".git");
        return File.Exists(gitPath) && !Directory.Exists(gitPath);
    }
}
