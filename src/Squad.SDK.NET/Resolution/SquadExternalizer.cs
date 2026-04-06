using System.Text.Json;

namespace Squad.SDK.NET.Resolution;

/// <summary>
/// Provides operations to externalize and internalize squad state between the working tree and
/// the user's platform-specific config directory.
///
/// <list type="bullet">
///   <item><description><see cref="Externalize"/> — moves <c>.squad/</c> state out of the working tree so it
///   survives branch switches and is invisible to <c>git status</c>.</description></item>
///   <item><description><see cref="Internalize"/> — moves externalized state back into <c>.squad/</c>.</description></item>
/// </list>
/// </summary>
public static class SquadExternalizer
{
    /// <summary>Squad sub-directories that hold operational state.</summary>
    private static readonly string[] StateDirs =
    [
        "agents", "casting", "decisions", "identity", "orchestration-log",
        "log", "plugins", "templates", "skills", "sessions", ".scratch",
    ];

    /// <summary>Squad files that hold state (not <c>config.json</c> — that stays in the working tree).</summary>
    private static readonly string[] StateFiles =
    [
        "team.md", "routing.md", "ceremonies.md", "decisions.md",
        "decisions-archive.md", ".first-run",
    ];

    /// <summary>
    /// Moves <c>.squad/</c> state to the platform-specific external directory and writes a thin
    /// <c>.squad/config.json</c> marker in the repo.
    ///
    /// After externalization:
    /// <list type="bullet">
    ///   <item><description>State survives branch switches (not tied to the working tree).</description></item>
    ///   <item><description>State is invisible to <c>git status</c> and never pollutes PRs.</description></item>
    ///   <item><description>A <c>.squad/config.json</c> marker lets the walk-up resolver find external state.</description></item>
    /// </list>
    /// </summary>
    /// <param name="projectDir">Absolute path to the project root (the directory that contains <c>.squad/</c>).</param>
    /// <param name="projectKey">Optional explicit project key. Defaults to the result of <see cref="SquadResolver.DeriveProjectKey"/>.</param>
    /// <returns>The absolute path to the external state directory.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <c>.squad/</c> does not exist under <paramref name="projectDir"/>.</exception>
    public static string Externalize(string projectDir, string? projectKey = null)
    {
        var squadDir = Path.Combine(projectDir, SquadResolver.SquadDirName);
        if (!Directory.Exists(squadDir))
            throw new InvalidOperationException(".squad/ directory not found. Run `squad init` first.");

        var key = projectKey ?? SquadResolver.DeriveProjectKey(projectDir);
        var externalDir = SquadResolver.ResolveExternalStateDir(key, create: true);

        var movedCount = 0;

        // Move directories
        foreach (var dir in StateDirs)
        {
            var src = Path.Combine(squadDir, dir);
            if (!Directory.Exists(src)) continue;
            var dest = Path.Combine(externalDir, dir);
            CopyDirRecursive(src, dest);
            Directory.Delete(src, recursive: true);
            movedCount++;
        }

        // Move files
        foreach (var file in StateFiles)
        {
            var src = Path.Combine(squadDir, file);
            if (!File.Exists(src)) continue;
            var dest = Path.Combine(externalDir, file);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(src, dest, overwrite: true);
            File.Delete(src);
            movedCount++;
        }

        // Write thin config.json marker, preserving any existing config fields
        var configPath = Path.Combine(squadDir, "config.json");
        var existingConfig = ReadConfigDict(configPath);

        existingConfig["version"] = 1;
        existingConfig["teamRoot"] = ".";
        existingConfig["projectKey"] = key;
        existingConfig["stateLocation"] = "external";

        File.WriteAllText(configPath, SerializeConfigDict(existingConfig) + Environment.NewLine);

        // Ensure config.json is gitignored (it's machine-local)
        EnsureGitignored(projectDir, ".squad/config.json");

        return externalDir;
    }

    /// <summary>
    /// Moves externalized state back into <c>.squad/</c> in the working tree.
    /// </summary>
    /// <param name="projectDir">Absolute path to the project root (the directory that contains <c>.squad/</c>).</param>
    /// <exception cref="InvalidOperationException">Thrown when <c>.squad/config.json</c> is missing, malformed, or state is already local.</exception>
    public static void Internalize(string projectDir)
    {
        var squadDir = Path.Combine(projectDir, SquadResolver.SquadDirName);
        var configPath = Path.Combine(squadDir, "config.json");

        if (!File.Exists(configPath))
            throw new InvalidOperationException(".squad/config.json not found. State is already local or not initialized.");

        Dictionary<string, object?> config;
        try
        {
            config = ReadConfigDict(configPath);
        }
        catch
        {
            throw new InvalidOperationException(".squad/config.json is malformed.");
        }

        if (!config.TryGetValue("stateLocation", out var loc) || loc?.ToString() != "external")
            throw new InvalidOperationException("State is already local (stateLocation is not \"external\").");

        var key = config.TryGetValue("projectKey", out var pk) && pk is not null
            ? pk.ToString()!
            : SquadResolver.DeriveProjectKey(projectDir);

        var externalDir = SquadResolver.ResolveExternalStateDir(key, create: false);
        if (!Directory.Exists(externalDir))
            throw new InvalidOperationException($"External state directory not found: {externalDir}");

        var movedCount = 0;

        // Copy directories back
        foreach (var dir in StateDirs)
        {
            var src = Path.Combine(externalDir, dir);
            if (!Directory.Exists(src)) continue;
            var dest = Path.Combine(squadDir, dir);
            CopyDirRecursive(src, dest);
            movedCount++;
        }

        // Copy files back
        foreach (var file in StateFiles)
        {
            var src = Path.Combine(externalDir, file);
            if (!File.Exists(src)) continue;
            File.Copy(src, Path.Combine(squadDir, file), overwrite: true);
            movedCount++;
        }

        // Remove external-state fields from config.json; delete the file if nothing meaningful remains
        config.Remove("stateLocation");
        config.Remove("teamRoot");
        config.Remove("projectKey");

        var versionKey = "version";
        var meaningfulKeys = config.Keys.Where(k => k != versionKey).ToList();
        if (meaningfulKeys.Count > 0)
        {
            File.WriteAllText(configPath, SerializeConfigDict(config) + Environment.NewLine);
        }
        else
        {
            File.Delete(configPath);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static void CopyDirRecursive(string src, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var entry in Directory.EnumerateFileSystemEntries(src))
        {
            var destEntry = Path.Combine(dest, Path.GetFileName(entry));
            if (Directory.Exists(entry))
                CopyDirRecursive(entry, destEntry);
            else
                File.Copy(entry, destEntry, overwrite: true);
        }
    }

    private static Dictionary<string, object?> ReadConfigDict(string configPath)
    {
        if (!File.Exists(configPath))
            return new Dictionary<string, object?>();

        var raw = File.ReadAllText(configPath);
        try
        {
            var doc = JsonDocument.Parse(raw);
            var dict = new Dictionary<string, object?>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.TryGetInt32(out var i) ? (object?)i : prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => prop.Value.GetRawText(),
                };
            }
            return dict;
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }

    private static string SerializeConfigDict(Dictionary<string, object?> dict)
    {
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true });
        writer.WriteStartObject();
        foreach (var (k, v) in dict)
        {
            switch (v)
            {
                case null:
                    writer.WriteNull(k);
                    break;
                case bool b:
                    writer.WriteBoolean(k, b);
                    break;
                case int i:
                    writer.WriteNumber(k, i);
                    break;
                case long l:
                    writer.WriteNumber(k, l);
                    break;
                case double d:
                    writer.WriteNumber(k, d);
                    break;
                case string s:
                    writer.WriteString(k, s);
                    break;
                default:
                    writer.WriteString(k, v.ToString() ?? "");
                    break;
            }
        }
        writer.WriteEndObject();
        writer.Flush();
        return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void EnsureGitignored(string projectDir, string entry)
    {
        var gitignorePath = Path.Combine(projectDir, ".gitignore");
        var existing = File.Exists(gitignorePath) ? File.ReadAllText(gitignorePath) : "";
        if (!existing.Contains(entry))
        {
            // Normalize: ensure we start on a new line regardless of CRLF/LF conventions
            var sep = (existing.Length > 0 && !existing.EndsWith('\n') && !existing.EndsWith('\r')) ? "\n" : "";
            var block = sep
                + "# Squad: local config (machine-specific, never commit)\n"
                + entry + "\n";
            File.AppendAllText(gitignorePath, block);
        }
    }
}
