using Squad.SDK.NET.Templates;

namespace Squad.SDK.NET.Tests;

public sealed class TemplateProviderTests : IDisposable
{
    private readonly string _tempDir;

    public TemplateProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"squad-templates-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string TempPath(string fileName) => Path.Combine(_tempDir, fileName);

    #region EnumerateTemplates

    [Fact]
    public void EnumerateTemplates_ContainsKnownAgentTemplate()
    {
        // Act
        var templates = TemplateProvider.EnumerateTemplates();

        // Assert
        Assert.NotEmpty(templates);
        Assert.Contains(templates, t => t.Name == TemplateProvider.SquadAgentTemplateName);
    }

    [Fact]
    public void EnumerateTemplates_AllItemsHaveValidProperties()
    {
        // Act
        var templates = TemplateProvider.EnumerateTemplates();

        // Assert
        foreach (var template in templates)
        {
            Assert.False(string.IsNullOrWhiteSpace(template.Name));
            Assert.False(string.IsNullOrWhiteSpace(template.ResourceName));
            Assert.StartsWith(TemplateProvider.ResourcePrefix, template.ResourceName);
        }
    }

    #endregion

    #region GetTemplate

    [Fact]
    public void GetTemplate_KnownTemplate_ReturnsTemplateInfo()
    {
        // Act
        var result = TemplateProvider.GetTemplate(TemplateProvider.SquadAgentTemplateName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TemplateProvider.SquadAgentTemplateName, result.Name);
        Assert.Equal(
            TemplateProvider.ResourcePrefix + TemplateProvider.SquadAgentTemplateName,
            result.ResourceName);
    }

    [Fact]
    public void GetTemplate_UnknownTemplate_ReturnsNull()
    {
        // Act
        var result = TemplateProvider.GetTemplate("nonexistent.template");

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetTemplate_NullOrWhitespace_ThrowsArgumentException(string? templateName)
    {
        // Act & Assert — ArgumentNullException (null) and ArgumentException (empty/ws) both derive from ArgumentException
        Assert.ThrowsAny<ArgumentException>(() => TemplateProvider.GetTemplate(templateName!));
    }

    #endregion

    #region TemplateInfo.OutputFileName

    [Fact]
    public void OutputFileName_WithTemplateSuffix_StripsSuffix()
    {
        // Arrange
        var info = new TemplateInfo
        {
            Name = "squad.agent.md.template",
            ResourceName = "Squad.SDK.NET.Templates.squad.agent.md.template"
        };

        // Act & Assert
        Assert.Equal("squad.agent.md", info.OutputFileName);
    }

    [Fact]
    public void OutputFileName_WithoutTemplateSuffix_ReturnsNameUnchanged()
    {
        // Arrange
        var info = new TemplateInfo
        {
            Name = "readme.md",
            ResourceName = "Squad.SDK.NET.Templates.readme.md"
        };

        // Act & Assert
        Assert.Equal("readme.md", info.OutputFileName);
    }

    [Fact]
    public void OutputFileName_CaseInsensitiveSuffix_StripsSuffix()
    {
        // Arrange
        var info = new TemplateInfo
        {
            Name = "chart.yaml.TEMPLATE",
            ResourceName = "Squad.SDK.NET.Templates.chart.yaml.TEMPLATE"
        };

        // Act & Assert
        Assert.Equal("chart.yaml", info.OutputFileName);
    }

    #endregion

    #region ExtractAsync (string overload)

    [Fact]
    public async Task ExtractAsync_ByName_WritesFileToTargetPath()
    {
        // Arrange
        var target = TempPath("agent.md");

        // Act
        await TemplateProvider.ExtractAsync(TemplateProvider.SquadAgentTemplateName, target);

        // Assert
        Assert.True(File.Exists(target));
        var content = await File.ReadAllTextAsync(target);
        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    [Fact]
    public async Task ExtractAsync_ByName_CreatesIntermediateDirectories()
    {
        // Arrange
        var target = Path.Combine(_tempDir, "sub", "deep", "agent.md");

        // Act
        await TemplateProvider.ExtractAsync(TemplateProvider.SquadAgentTemplateName, target);

        // Assert
        Assert.True(File.Exists(target));
    }

    [Fact]
    public async Task ExtractAsync_ByName_ExistingFileNoOverwrite_ThrowsIOException()
    {
        // Arrange
        var target = TempPath("existing.md");
        await File.WriteAllTextAsync(target, "original content");

        // Act & Assert
        await Assert.ThrowsAsync<IOException>(
            () => TemplateProvider.ExtractAsync(TemplateProvider.SquadAgentTemplateName, target, overwrite: false));

        // Verify original file unchanged
        Assert.Equal("original content", await File.ReadAllTextAsync(target));
    }

    [Fact]
    public async Task ExtractAsync_ByName_ExistingFileOverwriteTrue_ReplacesFile()
    {
        // Arrange
        var target = TempPath("overwritten.md");
        await File.WriteAllTextAsync(target, "old");

        // Act
        await TemplateProvider.ExtractAsync(TemplateProvider.SquadAgentTemplateName, target, overwrite: true);

        // Assert
        var content = await File.ReadAllTextAsync(target);
        Assert.NotEqual("old", content);
        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    [Fact]
    public async Task ExtractAsync_ByName_UnknownTemplate_ThrowsInvalidOperationException()
    {
        // Arrange
        var target = TempPath("nope.md");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => TemplateProvider.ExtractAsync("nonexistent.template", target));

        Assert.Contains("nonexistent.template", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExtractAsync_ByName_NullOrWhitespaceName_ThrowsArgumentException(string? name)
    {
        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => TemplateProvider.ExtractAsync(name!, TempPath("out.md")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExtractAsync_ByName_NullOrWhitespacePath_ThrowsArgumentException(string? path)
    {
        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => TemplateProvider.ExtractAsync(TemplateProvider.SquadAgentTemplateName, path!));
    }

    #endregion

    #region ExtractAsync (TemplateInfo overload)

    [Fact]
    public async Task ExtractAsync_ByInfo_WritesFileToTargetPath()
    {
        // Arrange
        var template = TemplateProvider.GetTemplate(TemplateProvider.SquadAgentTemplateName);
        Assert.NotNull(template);
        var target = TempPath("info-extract.md");

        // Act
        await TemplateProvider.ExtractAsync(template, target);

        // Assert
        Assert.True(File.Exists(target));
        var content = await File.ReadAllTextAsync(target);
        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    [Fact]
    public async Task ExtractAsync_ByInfo_NullTemplate_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => TemplateProvider.ExtractAsync((TemplateInfo)null!, TempPath("out.md")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExtractAsync_ByInfo_NullOrWhitespacePath_ThrowsArgumentException(string? path)
    {
        // Arrange
        var template = TemplateProvider.GetTemplate(TemplateProvider.SquadAgentTemplateName)!;

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => TemplateProvider.ExtractAsync(template, path!));
    }

    [Fact]
    public async Task ExtractAsync_ByInfo_ExistingFileNoOverwrite_ThrowsIOException()
    {
        // Arrange
        var template = TemplateProvider.GetTemplate(TemplateProvider.SquadAgentTemplateName)!;
        var target = TempPath("exists.md");
        await File.WriteAllTextAsync(target, "keep me");

        // Act & Assert
        await Assert.ThrowsAsync<IOException>(
            () => TemplateProvider.ExtractAsync(template, target, overwrite: false));

        Assert.Equal("keep me", await File.ReadAllTextAsync(target));
    }

    [Fact]
    public async Task ExtractAsync_ByInfo_OverwriteTrue_ReplacesFile()
    {
        // Arrange
        var template = TemplateProvider.GetTemplate(TemplateProvider.SquadAgentTemplateName)!;
        var target = TempPath("replace-me.md");
        await File.WriteAllTextAsync(target, "old content");

        // Act
        await TemplateProvider.ExtractAsync(template, target, overwrite: true);

        // Assert
        var content = await File.ReadAllTextAsync(target);
        Assert.NotEqual("old content", content);
        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    #endregion

    #region ScaffoldAgentDeclarationAsync

    [Fact]
    public async Task ScaffoldAgentDeclarationAsync_WritesToConventionalPath()
    {
        // Arrange — use a fresh subdirectory as the fake repo root
        var repoRoot = Path.Combine(_tempDir, "repo-scaffold");
        Directory.CreateDirectory(repoRoot);

        // Act
        var result = await TemplateProvider.ScaffoldAgentDeclarationAsync(repoRoot);

        // Assert — file lands at {repoRoot}/.github/agents/squad.agent.md
        var expected = Path.Combine(repoRoot, TemplateProvider.AgentDeclarationDirectory, TemplateProvider.AgentDeclarationFileName);
        Assert.Equal(expected, result);
        Assert.True(File.Exists(result));
        var content = await File.ReadAllTextAsync(result);
        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    [Fact]
    public async Task ScaffoldAgentDeclarationAsync_CreatesDirectoriesWhenMissing()
    {
        // Arrange — .github/agents does not exist yet
        var repoRoot = Path.Combine(_tempDir, "repo-no-dirs");
        Directory.CreateDirectory(repoRoot);
        var agentsDir = Path.Combine(repoRoot, TemplateProvider.AgentDeclarationDirectory);
        Assert.False(Directory.Exists(agentsDir));

        // Act
        var result = await TemplateProvider.ScaffoldAgentDeclarationAsync(repoRoot);

        // Assert
        Assert.True(Directory.Exists(agentsDir));
        Assert.True(File.Exists(result));
    }

    [Fact]
    public async Task ScaffoldAgentDeclarationAsync_ReturnsWrittenPath()
    {
        // Arrange
        var repoRoot = Path.Combine(_tempDir, "repo-return-path");
        Directory.CreateDirectory(repoRoot);

        // Act
        var result = await TemplateProvider.ScaffoldAgentDeclarationAsync(repoRoot);

        // Assert — returned path should match the conventional location
        Assert.EndsWith(
            Path.Combine(TemplateProvider.AgentDeclarationDirectory, TemplateProvider.AgentDeclarationFileName),
            result);
        Assert.True(File.Exists(result));
    }

    [Fact]
    public async Task ScaffoldAgentDeclarationAsync_ExistingFileNoOverwrite_ThrowsIOException()
    {
        // Arrange — pre-create the declaration file
        var repoRoot = Path.Combine(_tempDir, "repo-exists");
        var agentsDir = Path.Combine(repoRoot, TemplateProvider.AgentDeclarationDirectory);
        Directory.CreateDirectory(agentsDir);
        var existing = Path.Combine(agentsDir, TemplateProvider.AgentDeclarationFileName);
        await File.WriteAllTextAsync(existing, "original");

        // Act & Assert
        await Assert.ThrowsAsync<IOException>(
            () => TemplateProvider.ScaffoldAgentDeclarationAsync(repoRoot, overwrite: false));

        // Original file must be untouched
        Assert.Equal("original", await File.ReadAllTextAsync(existing));
    }

    [Fact]
    public async Task ScaffoldAgentDeclarationAsync_OverwriteTrue_ReplacesExistingFile()
    {
        // Arrange
        var repoRoot = Path.Combine(_tempDir, "repo-overwrite");
        var agentsDir = Path.Combine(repoRoot, TemplateProvider.AgentDeclarationDirectory);
        Directory.CreateDirectory(agentsDir);
        var existing = Path.Combine(agentsDir, TemplateProvider.AgentDeclarationFileName);
        await File.WriteAllTextAsync(existing, "stale");

        // Act
        var result = await TemplateProvider.ScaffoldAgentDeclarationAsync(repoRoot, overwrite: true);

        // Assert
        var content = await File.ReadAllTextAsync(result);
        Assert.NotEqual("stale", content);
        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ScaffoldAgentDeclarationAsync_NullOrWhitespaceRepoRoot_ThrowsArgumentException(string? repoRoot)
    {
        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => TemplateProvider.ScaffoldAgentDeclarationAsync(repoRoot!));
    }

    #endregion

    #region Public constants

    [Fact]
    public void AgentDeclarationDirectory_HasExpectedValue()
    {
        Assert.Equal(".github/agents", TemplateProvider.AgentDeclarationDirectory);
    }

    [Fact]
    public void AgentDeclarationFileName_HasExpectedValue()
    {
        Assert.Equal("squad.agent.md", TemplateProvider.AgentDeclarationFileName);
    }

    [Fact]
    public void AgentDeclarationFileName_MatchesTemplateOutputFileName()
    {
        // The convenience API should produce the same file name that OutputFileName yields
        var template = TemplateProvider.GetTemplate(TemplateProvider.SquadAgentTemplateName);
        Assert.NotNull(template);
        Assert.Equal(TemplateProvider.AgentDeclarationFileName, template.OutputFileName);
    }

    #endregion

    #region Enumeration / Lookup consistency

    [Fact]
    public void EnumerateTemplates_MatchesGetTemplate_ForKnownTemplate()
    {
        // Act
        var enumerated = TemplateProvider.EnumerateTemplates();
        var agent = enumerated.Single(t => t.Name == TemplateProvider.SquadAgentTemplateName);
        var looked = TemplateProvider.GetTemplate(TemplateProvider.SquadAgentTemplateName);

        // Assert
        Assert.NotNull(looked);
        Assert.Equal(agent.Name, looked.Name);
        Assert.Equal(agent.ResourceName, looked.ResourceName);
    }

    #endregion

    #region Nested / multi-template regression

    [Fact]
    public void EnumerateTemplates_AllNames_UseForwardSlashSeparators()
    {
        // Template names use forward-slash separators for nested paths
        // (e.g., "casting/Futurama.json", "skills/nap/SKILL.md").
        // Backslashes must never appear.
        var templates = TemplateProvider.EnumerateTemplates();

        foreach (var template in templates)
        {
            Assert.DoesNotContain("\\", template.Name);
        }
    }

    [Fact]
    public void EnumerateTemplates_NoDuplicateNames()
    {
        // Regression: if nested templates flatten to the same filename, this catches the collision.
        var templates = TemplateProvider.EnumerateTemplates();
        var names = templates.Select(t => t.Name).ToList();

        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void EnumerateTemplates_NoDuplicateResourceNames()
    {
        // Regression: resource names must be unique across the assembly.
        var templates = TemplateProvider.EnumerateTemplates();
        var resourceNames = templates.Select(t => t.ResourceName).ToList();

        Assert.Equal(resourceNames.Count, resourceNames.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void EnumerateTemplates_AllCanBeResolvedByGetTemplate()
    {
        // Regression: every enumerated template must round-trip through GetTemplate.
        var templates = TemplateProvider.EnumerateTemplates();
        Assert.NotEmpty(templates);

        foreach (var template in templates)
        {
            var looked = TemplateProvider.GetTemplate(template.Name);
            Assert.NotNull(looked);
            Assert.Equal(template.Name, looked.Name);
            Assert.Equal(template.ResourceName, looked.ResourceName);
        }
    }

    [Fact]
    public async Task EnumerateTemplates_AllCanBeExtracted()
    {
        // Regression: every enumerated template must extract non-empty content.
        // Nested template names contain forward slashes (e.g., "skills/nap/SKILL.md"),
        // so we sanitize the name for use as a flat disambiguator in the file path.
        var templates = TemplateProvider.EnumerateTemplates();
        Assert.NotEmpty(templates);

        foreach (var template in templates)
        {
            var safeName = template.Name.Replace('/', '_').Replace('\\', '_');
            var target = Path.Combine(_tempDir, $"extract-all-{safeName}");
            await TemplateProvider.ExtractAsync(template, target);

            Assert.True(File.Exists(target), $"Template '{template.Name}' did not extract.");
            var content = await File.ReadAllTextAsync(target);
            Assert.False(string.IsNullOrWhiteSpace(content), $"Template '{template.Name}' extracted empty content.");
        }
    }

    [Fact]
    public void EnumerateTemplates_AllHaveValidOutputFileName()
    {
        // Regression: OutputFileName must be non-empty and a valid file name for every template.
        var templates = TemplateProvider.EnumerateTemplates();

        foreach (var template in templates)
        {
            var output = template.OutputFileName;
            Assert.False(string.IsNullOrWhiteSpace(output), $"Template '{template.Name}' has blank OutputFileName.");
            Assert.DoesNotContain("/", output);
            Assert.DoesNotContain("\\", output);
            Assert.Equal(output.Trim(), output); // no leading/trailing whitespace
        }
    }

    [Fact]
    public void EnumerateTemplates_AllResourceNames_HaveCorrectPrefix()
    {
        // Regression: resource names must start with the well-known prefix.
        var templates = TemplateProvider.EnumerateTemplates();

        foreach (var template in templates)
        {
            Assert.StartsWith(TemplateProvider.ResourcePrefix, template.ResourceName);
            // Stripping the prefix must yield the Name
            var derived = template.ResourceName[TemplateProvider.ResourcePrefix.Length..];
            Assert.Equal(template.Name, derived);
        }
    }

    [Fact]
    public async Task ScaffoldAgentDeclarationAsync_UnaffectedByAdditionalTemplates()
    {
        // Regression: adding more templates must not break the scaffold convenience API.
        var repoRoot = Path.Combine(_tempDir, "repo-multi-template");
        Directory.CreateDirectory(repoRoot);

        // Verify enumeration has at least one template beyond the agent declaration
        var templates = TemplateProvider.EnumerateTemplates();
        Assert.Contains(templates, t => t.Name == TemplateProvider.SquadAgentTemplateName);

        // Act — scaffold must still work
        var result = await TemplateProvider.ScaffoldAgentDeclarationAsync(repoRoot);

        // Assert — conventional path and non-empty content
        var expected = Path.Combine(repoRoot, TemplateProvider.AgentDeclarationDirectory, TemplateProvider.AgentDeclarationFileName);
        Assert.Equal(expected, result);
        Assert.True(File.Exists(result));
        var content = await File.ReadAllTextAsync(result);
        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    #endregion

    #region TemplateInfo — OutputPath and OutputFileName for nested templates

    [Theory]
    [InlineData("squad.agent.md.template", "squad.agent.md")]
    [InlineData("skills/nap/SKILL.md", "skills/nap/SKILL.md")]
    [InlineData("casting/Futurama.json", "casting/Futurama.json")]
    [InlineData("workflows/squad-ci.yml", "workflows/squad-ci.yml")]
    [InlineData("agents/charter.md.template", "agents/charter.md")]
    public void OutputPath_PreservesDirectoryStructure(string name, string expectedOutputPath)
    {
        // OutputPath preserves forward-slash directory segments and strips only the .template suffix.
        var info = new TemplateInfo
        {
            Name = name,
            ResourceName = TemplateProvider.ResourcePrefix + name
        };

        Assert.Equal(expectedOutputPath, info.OutputPath);
    }

    [Theory]
    [InlineData("skills/nap/SKILL.md", "SKILL.md")]
    [InlineData("casting/Futurama.json", "Futurama.json")]
    [InlineData("agents/charter.md.template", "charter.md")]
    [InlineData("squad.agent.md.template", "squad.agent.md")]
    [InlineData("workflows/squad-ci.yml", "squad-ci.yml")]
    public void OutputFileName_ExtractsLeafFromOutputPath(string name, string expectedLeaf)
    {
        // OutputFileName returns only the leaf name (after the last '/') from OutputPath.
        var info = new TemplateInfo
        {
            Name = name,
            ResourceName = TemplateProvider.ResourcePrefix + name
        };

        Assert.Equal(expectedLeaf, info.OutputFileName);
    }

    [Theory]
    [InlineData("sub.foo.md.template", "sub.foo.md")]
    [InlineData("deep.nested.bar.yaml.template", "deep.nested.bar.yaml")]
    [InlineData("a.b.c.d.template", "a.b.c.d")]
    public void OutputPath_DottedRootName_StripsOnlyTemplateSuffix(string name, string expectedOutput)
    {
        // Dotted root-level names: OutputPath strips .template, nothing else.
        var info = new TemplateInfo
        {
            Name = name,
            ResourceName = TemplateProvider.ResourcePrefix + name
        };

        Assert.Equal(expectedOutput, info.OutputPath);
        // No directory component, so OutputFileName == OutputPath
        Assert.Equal(expectedOutput, info.OutputFileName);
    }

    [Theory]
    [InlineData("skills/test-discipline.md")]
    [InlineData("agents/squad.agent.md")]
    public void OutputPath_NameWithoutTemplateSuffix_ReturnsUnchanged(string name)
    {
        // Names that don't end in .template are returned as-is for OutputPath.
        var info = new TemplateInfo
        {
            Name = name,
            ResourceName = TemplateProvider.ResourcePrefix + name
        };

        Assert.Equal(name, info.OutputPath);
    }

    [Fact]
    public void OutputFileName_SingleDotTemplate_ProducesEmptyNamePrefix()
    {
        // Edge case: a name that is exactly ".template" — suffix stripping yields empty string.
        var info = new TemplateInfo
        {
            Name = ".template",
            ResourceName = TemplateProvider.ResourcePrefix + ".template"
        };

        Assert.Equal(string.Empty, info.OutputPath);
        Assert.Equal(string.Empty, info.OutputFileName);
    }

    [Fact]
    public void EnumerateTemplates_NestedTemplates_HaveForwardSlashesInName()
    {
        // Regression: nested templates must include directory segments in Name.
        var templates = TemplateProvider.EnumerateTemplates();
        var nested = templates.Where(t => t.Name.Contains('/')).ToList();

        Assert.NotEmpty(nested);

        // Known subtrees must be represented
        Assert.Contains(nested, t => t.Name.StartsWith("skills/", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nested, t => t.Name.StartsWith("casting/", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nested, t => t.Name.StartsWith("identity/", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nested, t => t.Name.StartsWith("workflows/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EnumerateTemplates_AllHaveValidOutputPath()
    {
        // OutputPath must be non-empty, use forward slashes, and have no leading/trailing whitespace.
        var templates = TemplateProvider.EnumerateTemplates();

        foreach (var template in templates)
        {
            var outputPath = template.OutputPath;
            Assert.False(string.IsNullOrWhiteSpace(outputPath), $"Template '{template.Name}' has blank OutputPath.");
            Assert.DoesNotContain("\\", outputPath);
            Assert.Equal(outputPath.Trim(), outputPath);
        }
    }

    #endregion

    #region EnumerateTemplates(string? pathPrefix) — subtree filtering

    [Fact]
    public void EnumerateTemplates_NullPrefix_ReturnsAllTemplates()
    {
        var all = TemplateProvider.EnumerateTemplates();
        var viaNull = TemplateProvider.EnumerateTemplates(null);

        Assert.Equal(all.Count, viaNull.Count);
    }

    [Fact]
    public void EnumerateTemplates_EmptyPrefix_ReturnsAllTemplates()
    {
        var all = TemplateProvider.EnumerateTemplates();
        var viaEmpty = TemplateProvider.EnumerateTemplates(string.Empty);

        Assert.Equal(all.Count, viaEmpty.Count);
    }

    [Fact]
    public void EnumerateTemplates_SkillsPrefix_ReturnsOnlySkillTemplates()
    {
        var skills = TemplateProvider.EnumerateTemplates("skills/");

        Assert.NotEmpty(skills);
        Assert.All(skills, t => Assert.StartsWith("skills/", t.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EnumerateTemplates_CastingPrefix_ReturnsCastingTemplates()
    {
        var casting = TemplateProvider.EnumerateTemplates("casting/");

        Assert.NotEmpty(casting);
        Assert.All(casting, t => Assert.StartsWith("casting/", t.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EnumerateTemplates_WorkflowsPrefix_ReturnsWorkflowTemplates()
    {
        var workflows = TemplateProvider.EnumerateTemplates("workflows/");

        Assert.NotEmpty(workflows);
        Assert.All(workflows, t => Assert.StartsWith("workflows/", t.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EnumerateTemplates_PrefixIsSubsetOfAll()
    {
        var all = TemplateProvider.EnumerateTemplates();
        var skills = TemplateProvider.EnumerateTemplates("skills/");

        Assert.True(skills.Count < all.Count, "Filtered subset must be smaller than full set.");
        Assert.All(skills, t => Assert.Contains(all, a => a.Name == t.Name));
    }

    [Fact]
    public void EnumerateTemplates_NonexistentPrefix_ReturnsEmpty()
    {
        var result = TemplateProvider.EnumerateTemplates("nonexistent-subtree/");

        Assert.Empty(result);
    }

    [Fact]
    public void EnumerateTemplates_BackslashPrefix_NormalizesToForwardSlash()
    {
        // NormalizePath converts backslashes, so "skills\\" should match "skills/" templates.
        var viaBackslash = TemplateProvider.EnumerateTemplates(@"skills\");
        var viaForward = TemplateProvider.EnumerateTemplates("skills/");

        Assert.Equal(viaForward.Count, viaBackslash.Count);
        Assert.NotEmpty(viaBackslash);
    }

    [Fact]
    public void EnumerateTemplates_PrefixIsCaseInsensitive()
    {
        var lower = TemplateProvider.EnumerateTemplates("skills/");
        var upper = TemplateProvider.EnumerateTemplates("SKILLS/");

        Assert.Equal(lower.Count, upper.Count);
    }

    #endregion

    #region Path normalization — backslash lookup via GetTemplate

    [Fact]
    public void GetTemplate_BackslashPath_NormalizesToForwardSlash()
    {
        // Find a known nested template via EnumerateTemplates, then look it up with backslashes.
        var templates = TemplateProvider.EnumerateTemplates("casting/");
        Assert.NotEmpty(templates);

        var expected = templates[0];
        var backslashName = expected.Name.Replace('/', '\\');
        var result = TemplateProvider.GetTemplate(backslashName);

        Assert.NotNull(result);
        Assert.Equal(expected.Name, result.Name);
        Assert.DoesNotContain("\\", result.Name);
    }

    [Fact]
    public void GetTemplate_ForwardSlashPath_WorksDirectly()
    {
        // Direct forward-slash lookup for a known nested template.
        var templates = TemplateProvider.EnumerateTemplates("identity/");
        Assert.NotEmpty(templates);

        var expected = templates[0];
        var result = TemplateProvider.GetTemplate(expected.Name);

        Assert.NotNull(result);
        Assert.Equal(expected.Name, result.Name);
    }

    #endregion

    #region ExtractToDirectoryAsync — bulk extraction with directory structure

    [Fact]
    public async Task ExtractToDirectoryAsync_AllTemplates_ExtractsToDirectory()
    {
        var targetDir = Path.Combine(_tempDir, "extract-all-dir");

        var paths = await TemplateProvider.ExtractToDirectoryAsync(targetDir);

        Assert.NotEmpty(paths);
        // Must extract the same number as EnumerateTemplates returns.
        var all = TemplateProvider.EnumerateTemplates();
        Assert.Equal(all.Count, paths.Count);
        Assert.All(paths, p => Assert.True(File.Exists(p), $"Extracted path does not exist: {p}"));
    }

    [Fact]
    public async Task ExtractToDirectoryAsync_AllTemplates_PreservesNestedDirectories()
    {
        var targetDir = Path.Combine(_tempDir, "extract-nested-dir");

        await TemplateProvider.ExtractToDirectoryAsync(targetDir);

        // Nested templates must create subdirectories under targetDir.
        var skillsDir = Path.Combine(targetDir, "skills");
        Assert.True(Directory.Exists(skillsDir), "skills/ subdirectory was not created.");

        var castingDir = Path.Combine(targetDir, "casting");
        Assert.True(Directory.Exists(castingDir), "casting/ subdirectory was not created.");
    }

    [Fact]
    public async Task ExtractToDirectoryAsync_AllTemplates_WrittenPathsAreOSNative()
    {
        var targetDir = Path.Combine(_tempDir, "extract-os-paths");

        var paths = await TemplateProvider.ExtractToDirectoryAsync(targetDir);

        // All returned paths must start with the target directory and use OS path separators.
        Assert.All(paths, p =>
        {
            Assert.StartsWith(targetDir, p);
            // On Windows, paths should use backslash; on Unix, forward slash.
            // The key invariant: no mixed separators in an OS path.
            if (Path.DirectorySeparatorChar == '\\')
                Assert.DoesNotContain("/", p);
        });
    }

    [Fact]
    public async Task ExtractToDirectoryAsync_AllTemplates_OverwriteFalse_ThrowsOnExisting()
    {
        var targetDir = Path.Combine(_tempDir, "extract-overwrite-false");

        // First extraction succeeds
        await TemplateProvider.ExtractToDirectoryAsync(targetDir, overwrite: true);

        // Second extraction without overwrite must throw
        await Assert.ThrowsAsync<IOException>(() =>
            TemplateProvider.ExtractToDirectoryAsync(targetDir, overwrite: false));
    }

    [Fact]
    public async Task ExtractToDirectoryAsync_AllTemplates_OverwriteTrue_Succeeds()
    {
        var targetDir = Path.Combine(_tempDir, "extract-overwrite-true");

        await TemplateProvider.ExtractToDirectoryAsync(targetDir, overwrite: true);
        // Second extraction with overwrite must succeed
        var paths = await TemplateProvider.ExtractToDirectoryAsync(targetDir, overwrite: true);

        Assert.NotEmpty(paths);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExtractToDirectoryAsync_NullOrWhitespaceTarget_ThrowsArgumentException(string? targetDir)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            TemplateProvider.ExtractToDirectoryAsync(targetDir!));
    }

    #endregion

    #region ExtractToDirectoryAsync(pathPrefix) — filtered bulk extraction

    [Fact]
    public async Task ExtractToDirectoryAsync_WithPrefix_ExtractsOnlyMatchingTemplates()
    {
        var targetDir = Path.Combine(_tempDir, "extract-prefix");

        var paths = await TemplateProvider.ExtractToDirectoryAsync(targetDir, "casting/");

        Assert.NotEmpty(paths);
        // Must be fewer than all templates
        var all = TemplateProvider.EnumerateTemplates();
        Assert.True(paths.Count < all.Count);
        // All written files must be under a casting subdirectory
        Assert.All(paths, p => Assert.Contains("casting", p));
    }

    [Fact]
    public async Task ExtractToDirectoryAsync_WithPrefix_PreservesSubdirectoryStructure()
    {
        var targetDir = Path.Combine(_tempDir, "extract-prefix-nested");

        await TemplateProvider.ExtractToDirectoryAsync(targetDir, "skills/");

        // skills/ templates are two levels deep (e.g., skills/nap/SKILL.md)
        var skillsDir = Path.Combine(targetDir, "skills");
        Assert.True(Directory.Exists(skillsDir));

        var subdirs = Directory.GetDirectories(skillsDir);
        Assert.NotEmpty(subdirs);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExtractToDirectoryAsync_WithPrefix_NullOrWhitespacePrefix_ThrowsArgumentException(string? prefix)
    {
        var targetDir = Path.Combine(_tempDir, "extract-prefix-null");

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            TemplateProvider.ExtractToDirectoryAsync(targetDir, prefix!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExtractToDirectoryAsync_WithPrefix_NullOrWhitespaceTarget_ThrowsArgumentException(string? targetDir)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            TemplateProvider.ExtractToDirectoryAsync(targetDir!, "skills/"));
    }

    [Fact]
    public async Task ExtractToDirectoryAsync_WithPrefix_BackslashPrefix_NormalizesAndMatches()
    {
        var targetDir = Path.Combine(_tempDir, "extract-prefix-backslash");

        var viaBackslash = await TemplateProvider.ExtractToDirectoryAsync(targetDir, @"casting\", overwrite: true);
        var viaForward = TemplateProvider.EnumerateTemplates("casting/");

        Assert.Equal(viaForward.Count, viaBackslash.Count);
    }

    #endregion
}
