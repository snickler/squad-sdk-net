using System.Text.Json;
using System.Text.Json.Serialization;
using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Presets;

/// <summary>
/// Merges preset agents into squad state files after a preset is applied.
/// </summary>
internal static class PresetApplyScaffold
{
    private const string MembersHeader = "## Members";
    private const string RoutingHeader = "## Work Type → Agent";

    internal static void Apply(string squadDir, IReadOnlyList<PresetAgent> agents, string presetName)
    {
        WriteOrMergeTeamMembers(squadDir, agents, presetName);
        WriteOrMergeRouting(squadDir, agents);
        WriteOrMergeCastingState(squadDir, agents, presetName);
    }

    private static void WriteOrMergeTeamMembers(string squadDir, IReadOnlyList<PresetAgent> agents, string presetName)
    {
        var teamPath = Path.Combine(squadDir, "team.md");
        var existing = File.Exists(teamPath) ? File.ReadAllText(teamPath) : string.Empty;
        var rows = agents.Select(MemberRow).ToArray();
        if (string.IsNullOrWhiteSpace(existing))
        {
            Directory.CreateDirectory(squadDir);
            File.WriteAllText(teamPath, string.Join('\n', new[]
            {
                "# Squad Team",
                string.Empty,
                $"> Created by `squad preset apply {presetName}`",
                string.Empty,
                MembersHeader,
                string.Empty,
                "| Name | Role | Charter | Status |",
                "|------|------|---------|--------|",
            }.Concat(rows).Concat(new[]
            {
                string.Empty,
            })));
            return;
        }

        MergeMarkdownTable(teamPath, MembersHeader, rows);
    }

    private static void WriteOrMergeRouting(string squadDir, IReadOnlyList<PresetAgent> agents)
    {
        var routingPath = Path.Combine(squadDir, "routing.md");
        var rows = agents.Select(RoutingRow).ToArray();
        if (!File.Exists(routingPath))
        {
            Directory.CreateDirectory(squadDir);
            File.WriteAllText(routingPath, string.Join('\n', new[]
            {
                "# Squad Routing",
                string.Empty,
                RoutingHeader,
                string.Empty,
                "| Work Type | Primary | Secondary |",
                "|-----------|---------|----------|",
            }.Concat(rows).Concat(new[]
            {
                string.Empty,
            })));
            return;
        }

        MergeMarkdownTable(routingPath, RoutingHeader, rows);
    }

    private static void WriteOrMergeCastingState(string squadDir, IReadOnlyList<PresetAgent> agents, string presetName)
    {
        var castingDir = Path.Combine(squadDir, "casting");
        Directory.CreateDirectory(castingDir);

        UpdateJsonArray(Path.Combine(castingDir, "registry.json"), agents.Select(a => new
        {
            name = a.Name,
            role = a.Role,
            preset = presetName,
        }).ToArray());

        UpdateJsonArray(Path.Combine(castingDir, "history.json"), Array.Empty<object>());
        UpdateJsonObject(Path.Combine(castingDir, "policy.json"), new
        {
            preset = presetName,
            agents = agents.Select(a => a.Name).ToArray(),
        });
    }

    private static void UpdateJsonArray(string path, object[] entries)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(entries, PresetApplyJsonContext.Default.ObjectArray));
    }

    private static void UpdateJsonObject(string path, object value)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(value, PresetApplyJsonContext.Default.Object));
    }

    private static string MemberRow(PresetAgent agent)
    {
        return $"| {agent.Name} | {agent.Role} | `.squad/agents/{agent.Name.ToLowerInvariant()}/charter.md` | {StatusForRole(agent.Role)} |";
    }

    private static string RoutingRow(PresetAgent agent)
    {
        return $"| {agent.Role} | {agent.Name} | — |";
    }

    private static string StatusForRole(string role)
    {
        var r = role.ToLowerInvariant();
        if (r is "session logger" or "scribe") return "📋 Silent";
        if (r is "work monitor" or "ralph") return "🔄 Monitor";
        if (r is "rai reviewer" or "rai") return "🛡️ RAI";
        if (r is "fact checker" or "fact-checker") return "🔍 Verifier";
        return "✅ Active";
    }

    private static void MergeMarkdownTable(string filePath, string sectionHeader, IReadOnlyList<string> newRows)
    {
        var existing = File.ReadAllText(filePath);
        if (newRows.Count == 0) return;
        if (!existing.Contains(sectionHeader, StringComparison.Ordinal))
        {
            File.AppendAllText(filePath, $"\n{sectionHeader}\n\n{string.Join('\n', newRows)}\n");
            return;
        }
        foreach (var row in newRows)
        {
            if (!existing.Contains(row, StringComparison.Ordinal))
                existing += row + "\n";
        }
        File.WriteAllText(filePath, existing);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(object[]))]
internal sealed partial class PresetApplyJsonContext : JsonSerializerContext
{
}
