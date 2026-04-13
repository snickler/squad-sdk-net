using Squad.SDK.NET.Templates;

namespace Squad.SDK.NET.Tests;

public sealed class RepositoryTemplateConsistencyTests : IDisposable
{
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

    private string RepoPath(params string[] segments)
        => Path.Combine(new[] { _repoRoot }.Concat(segments).ToArray());

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
