using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Squad.SDK.NET.Resolution;

namespace Squad.SDK.NET.Presets;

/// <summary>
/// Loads, applies, saves, and seeds Squad presets.
/// </summary>
/// <remarks>
/// Presets are curated agent collections stored in <c>&lt;squad-home&gt;/presets/&lt;name&gt;/</c>.
/// Each preset directory contains:
/// <list type="bullet">
///   <item><description><c>preset.json</c> — manifest with metadata and agent list</description></item>
///   <item><description><c>agents/&lt;name&gt;/charter.md</c> — agent charter files</description></item>
/// </list>
/// </remarks>
public static class PresetLoader
{
    // ========================================================================
    // Public API
    // ========================================================================

    /// <summary>
    /// Lists all available presets from the squad home presets directory.
    /// </summary>
    /// <returns>Array of preset manifests, or an empty array if no presets are found.</returns>
    public static PresetManifest[] ListPresets()
    {
        var presetsDir = SquadResolver.ResolvePresetsDir();
        if (presetsDir is null) return [];

        var presets = new List<PresetManifest>();
        foreach (var entry in Directory.EnumerateDirectories(presetsDir))
        {
            var manifest = LoadPresetManifest(entry);
            if (manifest is not null)
                presets.Add(manifest);
        }

        return [.. presets];
    }

    /// <summary>
    /// Loads a specific preset by name.
    /// </summary>
    /// <param name="name">Preset name (directory name under <c>presets/</c>).</param>
    /// <returns>The preset manifest, or <see langword="null"/> if not found.</returns>
    public static PresetManifest? LoadPreset(string name)
    {
        var presetsDir = SquadResolver.ResolvePresetsDir();
        if (presetsDir is null) return null;

        var presetDir = Path.Combine(presetsDir, name);
        if (!Directory.Exists(presetDir)) return null;

        return LoadPresetManifest(presetDir);
    }

    /// <summary>
    /// Applies a preset — copies its agents into a target squad directory.
    /// </summary>
    /// <remarks>
    /// By default, existing agents are skipped (not overwritten).
    /// Pass <paramref name="force"/> as <see langword="true"/> to overwrite existing agents.
    /// </remarks>
    /// <param name="presetName">Name of the preset to apply.</param>
    /// <param name="targetDir">Target directory to install agents into (e.g. <c>.squad/agents/</c>).</param>
    /// <param name="force">When <see langword="true"/>, overwrites existing agent directories.</param>
    /// <returns>Array of results for each agent in the preset.</returns>
    public static PresetApplyResult[] ApplyPreset(string presetName, string targetDir, bool force = false)
    {
        try { ValidateName(presetName, "preset"); }
        catch (Exception ex)
        {
            return [new PresetApplyResult { Agent = presetName, Status = PresetApplyStatus.Error, Reason = ex.Message }];
        }

        var presetsDir = SquadResolver.ResolvePresetsDir();
        if (presetsDir is null)
        {
            return [new PresetApplyResult
            {
                Agent = presetName,
                Status = PresetApplyStatus.Error,
                Reason = "No presets directory found. Run 'squad preset init' to set up squad home."
            }];
        }

        var presetDir = Path.Combine(presetsDir, presetName);
        var manifest = LoadPresetManifest(presetDir);
        if (manifest is null)
        {
            return [new PresetApplyResult
            {
                Agent = presetName,
                Status = PresetApplyStatus.Error,
                Reason = $"Preset '{presetName}' not found."
            }];
        }

        var presetAgentsDir = Path.Combine(presetDir, "agents");
        var results = new List<PresetApplyResult>();

        foreach (var agent in manifest.Agents)
        {
            try { ValidateName(agent.Name, "agent"); }
            catch
            {
                results.Add(new PresetApplyResult
                {
                    Agent = agent.Name,
                    Status = PresetApplyStatus.Error,
                    Reason = $"Invalid agent name: '{agent.Name}'"
                });
                continue;
            }

            var sourceDir = Path.Combine(presetAgentsDir, agent.Name);
            var destDir = Path.Combine(targetDir, agent.Name);

            if (!Directory.Exists(sourceDir))
            {
                results.Add(new PresetApplyResult
                {
                    Agent = agent.Name,
                    Status = PresetApplyStatus.Error,
                    Reason = "Source agent directory missing in preset."
                });
                continue;
            }

            if (Directory.Exists(destDir) && !force)
            {
                results.Add(new PresetApplyResult
                {
                    Agent = agent.Name,
                    Status = PresetApplyStatus.Skipped,
                    Reason = "Already exists (use force to overwrite)."
                });
                continue;
            }

            try
            {
                if (force && Directory.Exists(destDir))
                    Directory.Delete(destDir, recursive: true);

                CopyDirectoryRecursive(sourceDir, destDir);
                results.Add(new PresetApplyResult { Agent = agent.Name, Status = PresetApplyStatus.Installed });
            }
            catch (Exception ex)
            {
                results.Add(new PresetApplyResult
                {
                    Agent = agent.Name,
                    Status = PresetApplyStatus.Error,
                    Reason = ex.Message
                });
            }
        }

        return [.. results];
    }

    /// <summary>
    /// Installs a preset into squad home from an external source directory.
    /// Copies the preset directory into <c>&lt;squad-home&gt;/presets/&lt;name&gt;/</c>.
    /// </summary>
    /// <param name="sourceDir">Source directory containing <c>preset.json</c> and <c>agents/</c>.</param>
    /// <param name="name">Preset name (used as destination directory name).</param>
    /// <returns>Path to the installed preset directory.</returns>
    public static string InstallPreset(string sourceDir, string name)
    {
        var homeDir = SquadResolver.EnsureSquadHome();
        var destDir = Path.Combine(homeDir, "presets", name);

        CopyDirectoryRecursive(sourceDir, destDir);
        return destDir;
    }

    /// <summary>
    /// Saves the current project's squad agents as a reusable preset.
    /// </summary>
    /// <remarks>
    /// Reads agents from the given squad directory (e.g. <c>.squad/agents/</c>),
    /// generates a <c>preset.json</c> manifest, and copies everything into
    /// <c>&lt;squad-home&gt;/presets/&lt;name&gt;/</c>.
    /// </remarks>
    /// <param name="name">Name for the new preset.</param>
    /// <param name="squadDir">Path to the project's <c>.squad/</c> directory containing <c>agents/</c>.</param>
    /// <param name="force">When <see langword="true"/>, overwrites an existing preset with the same name.</param>
    /// <param name="description">Optional description for the preset manifest.</param>
    /// <returns>Path to the saved preset directory.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no agents are found or the preset already exists (without <paramref name="force"/>).</exception>
    public static string SavePreset(string name, string squadDir, bool force = false, string? description = null)
    {
        ValidateName(name, "preset");

        var agentsDir = Path.Combine(squadDir, "agents");
        if (!Directory.Exists(agentsDir))
            throw new InvalidOperationException($"No agents/ directory found in {squadDir}.");

        var homeDir = SquadResolver.EnsureSquadHome();
        var destDir = Path.Combine(homeDir, "presets", name);

        if (Directory.Exists(destDir) && !force)
            throw new InvalidOperationException($"Preset '{name}' already exists. Use force to overwrite.");

        var agentEntries = Directory.EnumerateDirectories(agentsDir).ToList();
        var agents = new List<PresetAgent>();

        foreach (var agentDir in agentEntries)
        {
            var agentName = Path.GetFileName(agentDir);
            var role = agentName;
            var desc = string.Empty;

            var charterPath = Path.Combine(agentDir, "charter.md");
            if (File.Exists(charterPath))
            {
                var content = File.ReadAllText(charterPath);
                // Try to extract role from the first heading line (e.g. "# lead — Technical Lead")
                var roleMatch = System.Text.RegularExpressions.Regex.Match(content, @"^##?\s+.*?[-–—]\s*(.+?)$", System.Text.RegularExpressions.RegexOptions.Multiline);
                if (roleMatch.Success)
                    role = roleMatch.Groups[1].Value.Trim();

                // First non-empty, non-heading line as description
                foreach (var line in content.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith('#') && !trimmed.StartsWith("---"))
                    {
                        desc = trimmed.Length > MaxDescriptionLength ? trimmed[..MaxDescriptionLength] : trimmed;
                        break;
                    }
                }
            }

            agents.Add(new PresetAgent { Name = agentName, Role = role, Description = desc });
        }

        if (agents.Count == 0)
            throw new InvalidOperationException($"No agents found in {agentsDir}.");

        var manifest = new PresetManifest
        {
            Name = name,
            Version = "1.0.0",
            Description = description ?? $"Custom preset '{name}'",
            Agents = agents
        };

        Directory.CreateDirectory(destDir);
        var manifestJson = JsonSerializer.Serialize(manifest, PresetJsonContext.Default.PresetManifest);
        File.WriteAllText(Path.Combine(destDir, "preset.json"), manifestJson);
        CopyDirectoryRecursive(agentsDir, Path.Combine(destDir, "agents"));

        return destDir;
    }

    /// <summary>
    /// Seeds squad home with built-in presets that ship with the SDK.
    /// Only copies presets that are missing — never overwrites user presets.
    /// </summary>
    /// <returns>Names of presets that were seeded.</returns>
    public static string[] SeedBuiltinPresets()
    {
        var homeDir = SquadResolver.EnsureSquadHome();
        var targetPresetsDir = Path.Combine(homeDir, "presets");
        var seeded = new List<string>();

        var assembly = typeof(PresetLoader).Assembly;
        const string builtinPrefix = "Squad.SDK.NET.Presets.Builtin.";

        // Resources are named: "Squad.SDK.NET.Presets.Builtin.{presetName}/{relative/path}"
        // e.g. "Squad.SDK.NET.Presets.Builtin.default/preset.json"
        //      "Squad.SDK.NET.Presets.Builtin.default/agents/lead/charter.md"
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(builtinPrefix, StringComparison.Ordinal))
            .ToArray();

        // Group by preset name (the segment between the prefix and the first '/')
        var presetGroups = resourceNames
            .Select(n =>
            {
                var rel = n[builtinPrefix.Length..];              // e.g. "default/preset.json"
                var slashIdx = rel.IndexOf('/');
                return slashIdx < 0
                    ? (presetName: rel, relPath: string.Empty, resourceName: n)
                    : (presetName: rel[..slashIdx], relPath: rel[(slashIdx + 1)..], resourceName: n);
            })
            .GroupBy(x => x.presetName)
            .ToArray();

        foreach (var group in presetGroups)
        {
            var presetName = group.Key;
            var destDir = Path.Combine(targetPresetsDir, presetName);

            // Skip if already installed
            if (Directory.Exists(destDir)) continue;

            Directory.CreateDirectory(destDir);

            foreach (var (_, relPath, resourceName) in group)
            {
                if (string.IsNullOrEmpty(relPath)) continue;

                // Convert forward slashes to platform path separators
                var filePath = Path.Combine(destDir, relPath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream is null) continue;

                using var reader = new StreamReader(stream);
                File.WriteAllText(filePath, reader.ReadToEnd());
            }

            seeded.Add(presetName);
        }

        return [.. seeded];
    }

    // ========================================================================
    // Internal helpers
    // ========================================================================

    private const int MaxDescriptionLength = 120;

    private static PresetManifest? LoadPresetManifest(string presetDir)
    {
        var manifestPath = Path.Combine(presetDir, "preset.json");
        if (!File.Exists(manifestPath)) return null;

        try
        {
            var content = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize(content, PresetJsonContext.Default.PresetManifest);
            if (manifest is null || string.IsNullOrEmpty(manifest.Name) || manifest.Agents is null || manifest.Agents.Count == 0)
                return null;

            return manifest;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void ValidateName(string name, string label)
    {
        if (string.IsNullOrEmpty(name)
            || name != Path.GetFileName(name)
            || name == ".."
            || name == ".")
        {
            throw new ArgumentException($"Invalid {label} name: '{name}'. Must be a simple directory name.", nameof(name));
        }
    }

    private static void CopyDirectoryRecursive(string source, string dest)
    {
        Directory.CreateDirectory(dest);

        foreach (var file in Directory.EnumerateFiles(source))
        {
            // Verify no symlinks are followed
            var info = new FileInfo(file);
            if ((info.Attributes & FileAttributes.ReparsePoint) != 0) continue;

            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var dir in Directory.EnumerateDirectories(source))
        {
            var dirInfo = new DirectoryInfo(dir);
            if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0) continue;

            CopyDirectoryRecursive(dir, Path.Combine(dest, dirInfo.Name));
        }
    }
}

/// <summary>Source-generated JSON context for preset serialization.</summary>
[JsonSerializable(typeof(PresetManifest))]
[JsonSerializable(typeof(PresetAgent))]
[JsonSerializable(typeof(List<PresetAgent>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
internal sealed partial class PresetJsonContext : JsonSerializerContext
{
}
