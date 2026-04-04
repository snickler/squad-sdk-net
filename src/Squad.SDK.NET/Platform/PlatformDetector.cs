namespace Squad.SDK.NET.Platform;

public static class PlatformDetector
{
    public static PlatformType Detect(string? workingDir = null)
    {
        var dir = workingDir ?? Directory.GetCurrentDirectory();

        // Check for GitHub
        if (HasGitRemote(dir, "github.com"))
            return PlatformType.GitHub;

        // Check for Azure DevOps
        if (HasGitRemote(dir, "dev.azure.com") || HasGitRemote(dir, "visualstudio.com"))
            return PlatformType.AzureDevOps;

        // Check for .git directory (local git)
        if (Directory.Exists(Path.Combine(dir, ".git")) || File.Exists(Path.Combine(dir, ".git")))
            return PlatformType.Local;

        return PlatformType.Unknown;
    }

    private static bool HasGitRemote(string dir, string hostPattern)
    {
        try
        {
            var gitConfigPath = Path.Combine(dir, ".git", "config");
            if (!File.Exists(gitConfigPath))
            {
                // Could be a worktree — check .git file
                var gitFile = Path.Combine(dir, ".git");
                if (File.Exists(gitFile))
                {
                    var content = File.ReadAllText(gitFile).Trim();
                    if (content.StartsWith("gitdir:", StringComparison.OrdinalIgnoreCase))
                    {
                        var gitDir = content["gitdir:".Length..].Trim();
                        gitConfigPath = Path.Combine(gitDir, "..", "..", "config");
                        if (!File.Exists(gitConfigPath))
                            gitConfigPath = Path.Combine(gitDir, "config");
                    }
                }
            }

            if (!File.Exists(gitConfigPath))
                return false;

            var configContent = File.ReadAllText(gitConfigPath);
            return configContent.Contains(hostPattern, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
