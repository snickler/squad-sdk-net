using Microsoft.Extensions.Logging;

namespace Squad.SDK.NET.Resolution;

public sealed class MultiSquadManager
{
    private readonly ILogger<MultiSquadManager> _logger;

    public MultiSquadManager(ILogger<MultiSquadManager> logger)
    {
        _logger = logger;
    }

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

    public string CreateSquad(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Squad name is required.", nameof(name));

        if (name.Contains(Path.DirectorySeparatorChar) || name.Contains(Path.AltDirectorySeparatorChar) || name.Contains(".."))
            throw new ArgumentException("Squad name must not contain path separators or '..'.", nameof(name));

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

    public void DeleteSquad(string name)
    {
        var personalDir = SquadResolver.ResolvePersonalSquadDir();
        if (personalDir is null)
            throw new InvalidOperationException("Cannot determine personal squad directory.");

        if (name.Contains(Path.DirectorySeparatorChar) || name.Contains(Path.AltDirectorySeparatorChar) || name.Contains(".."))
            throw new ArgumentException("Squad name must not contain path separators or '..'.", nameof(name));

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
