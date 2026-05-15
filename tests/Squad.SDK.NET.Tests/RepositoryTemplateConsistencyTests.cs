using Squad.SDK.NET.Templates;

namespace Squad.SDK.NET.Tests;

public sealed class RepositoryTemplateConsistencyTests : IDisposable
{
    private static readonly string[] SkillFilesWithPortedFrontmatter =
    [
        ".copilot/skills/cli-wiring/SKILL.md",
        ".copilot/skills/model-selection/SKILL.md",
        ".copilot/skills/nap/SKILL.md",
        ".copilot/skills/personal-squad/SKILL.md",
        ".squad/templates/skills/cli-wiring/SKILL.md",
        ".squad/templates/skills/model-selection/SKILL.md",
        ".squad/templates/skills/nap/SKILL.md",
        ".squad/templates/skills/personal-squad/SKILL.md",
        "src/Squad.SDK.NET/Templates/skills/cli-wiring/SKILL.md",
        "src/Squad.SDK.NET/Templates/skills/cross-machine-coordination/SKILL.md",
        "src/Squad.SDK.NET/Templates/skills/model-selection/SKILL.md",
        "src/Squad.SDK.NET/Templates/skills/nap/SKILL.md",
        "src/Squad.SDK.NET/Templates/skills/personal-squad/SKILL.md",
        "src/Squad.SDK.NET/Templates/skills/ralph-two-pass-scan/SKILL.md"
    ];

    private static readonly string[] ReleaseProcessSkillFiles =
    [
        ".copilot/skills/release-process/SKILL.md",
        ".squad/templates/skills/release-process/SKILL.md",
        "src/Squad.SDK.NET/Templates/skills/release-process/SKILL.md"
    ];

    private readonly string _tempDir;
    private readonly string _repoRoot;

    public RepositoryTemplateConsistencyTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"squad-template-regression-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _repoRoot = FindRepositoryRoot();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task ScaffoldAgentDeclarationAsync_UpdatedTemplateIncludesDateContextAndDelegationGuard()
    {
        var repoRoot = Path.Combine(_tempDir, "repo");
        Directory.CreateDirectory(repoRoot);

        var scaffoldedPath = await TemplateProvider.ScaffoldAgentDeclarationAsync(repoRoot);
        var content = await File.ReadAllTextAsync(scaffoldedPath);

        Assert.Contains("CURRENT_DATETIME", content);
        Assert.Contains("ALWAYS delegate to a team member", content);
        Assert.False(content.Contains("mcp-tool-discovery", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void InstalledSquadAgentTemplates_UseCurrentDatetimeAndDropRetiredMcpDiscoverySkill()
    {
        foreach (var path in new[]
                 {
                     RepoPath(".github", "agents", "squad.agent.md"),
                     RepoPath(".squad", "templates", "squad.agent.md")
                 })
        {
            var content = File.ReadAllText(path);

            Assert.Contains("CURRENT_DATETIME", content);
            Assert.Contains("ALWAYS delegate to a team member", content);
            Assert.False(content.Contains("mcp-tool-discovery", StringComparison.OrdinalIgnoreCase), $"Template '{path}' still references the retired MCP discovery skill.");
        }
    }

    [Fact]
    public void ScribeCharterTemplate_UsesCurrentDatetimePlaceholderInsteadOfToday()
    {
        var content = File.ReadAllText(RepoPath(".squad", "templates", "scribe-charter.md"));

        Assert.Contains("CURRENT_DATETIME", content);
        Assert.False(content.Contains("{today}", StringComparison.Ordinal));
    }

    [Fact]
    public void PortedSkillFiles_IncludeYamlFrontmatter()
    {
        foreach (var relativePath in SkillFilesWithPortedFrontmatter)
        {
            var content = File.ReadAllText(RepoPath(relativePath));

            Assert.StartsWith("---", content);
            Assert.Contains("\nname:", content, StringComparison.Ordinal);
            Assert.Contains("\ndescription:", content, StringComparison.Ordinal);
            Assert.Contains("\ndomain:", content, StringComparison.Ordinal);
            Assert.Contains("\nconfidence:", content, StringComparison.Ordinal);
            Assert.Contains("\nsource:", content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ReleaseProcessSkills_DescribeChangesetDrivenPromotionFlow()
    {
        foreach (var relativePath in ReleaseProcessSkillFiles)
        {
            var content = File.ReadAllText(RepoPath(relativePath));

            Assert.Contains("dev -> preview -> main", content, StringComparison.Ordinal);
            Assert.Contains(".changeset", content, StringComparison.Ordinal);
            Assert.Contains("v<Version>", content, StringComparison.Ordinal);
        }
    }

    private string RepoPath(params string[] segments)
        => Path.Combine(new[] { _repoRoot }.Concat(segments).ToArray());

    private string RepoPath(string relativePath)
        => RepoPath(relativePath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries));

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Squad.SDK.NET.slnx")))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root for template consistency tests.");
    }
}
