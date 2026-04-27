using Squad.SDK.NET.Presets;
using Squad.SDK.NET.Resolution;

namespace Squad.SDK.NET.Tests;

#region Presets — SquadResolver SQUAD_HOME support

public sealed class SquadResolverSquadHomeTests : IDisposable
{
    private readonly string? _savedSquadHome;

    public SquadResolverSquadHomeTests()
    {
        _savedSquadHome = Environment.GetEnvironmentVariable("SQUAD_HOME");
    }

    public void Dispose()
    {
        // Restore the original environment variable after each test
        if (_savedSquadHome is null)
            Environment.SetEnvironmentVariable("SQUAD_HOME", null);
        else
            Environment.SetEnvironmentVariable("SQUAD_HOME", _savedSquadHome);
    }

    [Fact]
    public void ResolveSquadHome_NoEnvVar_ReturnsNullWhenDirMissing()
    {
        // Ensure we don't accidentally hit a real ~/.squad that exists on the test agent
        Environment.SetEnvironmentVariable("SQUAD_HOME", null);

        // We cannot guarantee ~/.squad doesn't exist, so only assert null when it truly doesn't
        var homeDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".squad");
        if (!Directory.Exists(homeDir))
        {
            var result = SquadResolver.ResolveSquadHome();
            Assert.Null(result);
        }
    }

    [Fact]
    public void ResolveSquadHome_EnvVarPointsToExistingDir_ReturnsThatDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"squad-home-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);
            Environment.SetEnvironmentVariable("SQUAD_HOME", tempDir);

            var result = SquadResolver.ResolveSquadHome();

            Assert.NotNull(result);
            Assert.Equal(Path.GetFullPath(tempDir), result);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ResolveSquadHome_EnvVarPointsToMissingDir_WithCreateFalse_ReturnsNull()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"squad-home-{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable("SQUAD_HOME", tempDir);

        var result = SquadResolver.ResolveSquadHome(create: false);

        Assert.Null(result);
        Assert.False(Directory.Exists(tempDir));
    }

    [Fact]
    public void ResolveSquadHome_EnvVarSet_WithCreateTrue_CreatesAndReturnsDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"squad-home-{Guid.NewGuid():N}");
        try
        {
            Environment.SetEnvironmentVariable("SQUAD_HOME", tempDir);

            var result = SquadResolver.ResolveSquadHome(create: true);

            Assert.NotNull(result);
            Assert.True(Directory.Exists(result));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ResolveSquadHome_EnvVarPointsToFile_ThrowsInvalidOperationException()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            Environment.SetEnvironmentVariable("SQUAD_HOME", tempFile);

            Assert.Throws<InvalidOperationException>(() => SquadResolver.ResolveSquadHome());
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void EnsureSquadHome_CreatesAgentsAndPresetsSubdirs()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"squad-home-{Guid.NewGuid():N}");
        try
        {
            Environment.SetEnvironmentVariable("SQUAD_HOME", tempDir);

            var result = SquadResolver.EnsureSquadHome();

            Assert.True(Directory.Exists(Path.Combine(result, "agents")));
            Assert.True(Directory.Exists(Path.Combine(result, "presets")));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void EnsureSquadHome_IsIdempotent_DoesNotThrow()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"squad-home-{Guid.NewGuid():N}");
        try
        {
            Environment.SetEnvironmentVariable("SQUAD_HOME", tempDir);

            var ex = Record.Exception(() =>
            {
                SquadResolver.EnsureSquadHome();
                SquadResolver.EnsureSquadHome();
                SquadResolver.EnsureSquadHome();
            });

            Assert.Null(ex);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ResolvePresetsDir_WhenPresetsSubdirExists_ReturnsPath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"squad-home-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(Path.Combine(tempDir, "presets"));
            Environment.SetEnvironmentVariable("SQUAD_HOME", tempDir);

            var result = SquadResolver.ResolvePresetsDir();

            Assert.NotNull(result);
            Assert.True(Directory.Exists(result));
            Assert.EndsWith("presets", result, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ResolvePresetsDir_WhenSquadHomeMissing_ReturnsNull()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"squad-home-{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable("SQUAD_HOME", tempDir);

        var result = SquadResolver.ResolvePresetsDir();

        Assert.Null(result);
    }

    [Fact]
    public void ResolvePresetsDir_WhenNoPresetSubdir_ReturnsNull()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"squad-home-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);
            // No presets/ subdirectory
            Environment.SetEnvironmentVariable("SQUAD_HOME", tempDir);

            var result = SquadResolver.ResolvePresetsDir();

            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}

#endregion

#region Presets — PresetManifest types

public sealed class PresetManifestTests
{
    [Fact]
    public void PresetAgent_Properties_AreSetCorrectly()
    {
        var agent = new PresetAgent { Name = "lead", Role = "lead", Description = "Technical lead" };

        Assert.Equal("lead", agent.Name);
        Assert.Equal("lead", agent.Role);
        Assert.Equal("Technical lead", agent.Description);
    }

    [Fact]
    public void PresetManifest_Properties_AreSetCorrectly()
    {
        var agents = new List<PresetAgent> { new() { Name = "lead", Role = "lead" } };
        var manifest = new PresetManifest
        {
            Name = "test",
            Version = "1.0.0",
            Description = "Test preset",
            Agents = agents
        };

        Assert.Equal("test", manifest.Name);
        Assert.Equal("1.0.0", manifest.Version);
        Assert.Equal("Test preset", manifest.Description);
        Assert.Single(manifest.Agents);
    }

    [Fact]
    public void PresetApplyResult_InstalledStatus_IsSet()
    {
        var result = new PresetApplyResult { Agent = "lead", Status = PresetApplyStatus.Installed };

        Assert.Equal("lead", result.Agent);
        Assert.Equal(PresetApplyStatus.Installed, result.Status);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void PresetApplyResult_SkippedStatus_IncludesReason()
    {
        var result = new PresetApplyResult
        {
            Agent = "reviewer",
            Status = PresetApplyStatus.Skipped,
            Reason = "Already exists (use force to overwrite)."
        };

        Assert.Equal(PresetApplyStatus.Skipped, result.Status);
        Assert.NotNull(result.Reason);
    }
}

#endregion

#region Presets — PresetLoader

public sealed class PresetLoaderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string? _savedSquadHome;

    public PresetLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"squad-preset-tests-{Guid.NewGuid():N}");
        _savedSquadHome = Environment.GetEnvironmentVariable("SQUAD_HOME");
    }

    public void Dispose()
    {
        if (_savedSquadHome is null)
            Environment.SetEnvironmentVariable("SQUAD_HOME", null);
        else
            Environment.SetEnvironmentVariable("SQUAD_HOME", _savedSquadHome);

        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private void SetupSquadHome(bool withPresetsDir = true)
    {
        Directory.CreateDirectory(_tempDir);
        if (withPresetsDir)
            Directory.CreateDirectory(Path.Combine(_tempDir, "presets"));
        Environment.SetEnvironmentVariable("SQUAD_HOME", _tempDir);
    }

    private void WritePreset(string presetName, string? manifestJson = null, string[]? agentNames = null)
    {
        var presetDir = Path.Combine(_tempDir, "presets", presetName);
        var agentsDir = Path.Combine(presetDir, "agents");
        Directory.CreateDirectory(presetDir);
        Directory.CreateDirectory(agentsDir);

        var agents = agentNames ?? ["lead", "reviewer"];
        var agentList = string.Join(",\n    ", agents.Select(a =>
            $"{{\"name\": \"{a}\", \"role\": \"{a}\", \"description\": \"{a} agent\"}}"));

        var json = manifestJson ?? $$"""
            {
              "name": "{{presetName}}",
              "version": "1.0.0",
              "description": "Test preset {{presetName}}",
              "agents": [
                {{agentList}}
              ]
            }
            """;

        File.WriteAllText(Path.Combine(presetDir, "preset.json"), json);

        foreach (var agentName in agents)
        {
            var agentDir = Path.Combine(agentsDir, agentName);
            Directory.CreateDirectory(agentDir);
            File.WriteAllText(Path.Combine(agentDir, "charter.md"), $"# {agentName} — Test Agent\n");
        }
    }

    [Fact]
    public void ListPresets_NoSquadHome_ReturnsEmpty()
    {
        var missingDir = Path.Combine(Path.GetTempPath(), $"squad-missing-{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable("SQUAD_HOME", missingDir);

        var presets = PresetLoader.ListPresets();

        Assert.Empty(presets);
    }

    [Fact]
    public void ListPresets_EmptyPresetsDir_ReturnsEmpty()
    {
        SetupSquadHome();

        var presets = PresetLoader.ListPresets();

        Assert.Empty(presets);
    }

    [Fact]
    public void ListPresets_WithOnePreset_ReturnsThatPreset()
    {
        SetupSquadHome();
        WritePreset("mypreset");

        var presets = PresetLoader.ListPresets();

        Assert.Single(presets);
        Assert.Equal("mypreset", presets[0].Name);
    }

    [Fact]
    public void ListPresets_WithMultiplePresets_ReturnsAll()
    {
        SetupSquadHome();
        WritePreset("alpha");
        WritePreset("beta");

        var presets = PresetLoader.ListPresets();

        Assert.Equal(2, presets.Length);
    }

    [Fact]
    public void LoadPreset_ExistingPreset_ReturnsManifest()
    {
        SetupSquadHome();
        WritePreset("mypreset");

        var manifest = PresetLoader.LoadPreset("mypreset");

        Assert.NotNull(manifest);
        Assert.Equal("mypreset", manifest!.Name);
        Assert.NotEmpty(manifest.Agents);
    }

    [Fact]
    public void LoadPreset_NonExistentPreset_ReturnsNull()
    {
        SetupSquadHome();

        var manifest = PresetLoader.LoadPreset("doesnotexist");

        Assert.Null(manifest);
    }

    [Fact]
    public void LoadPreset_NoSquadHome_ReturnsNull()
    {
        var missingDir = Path.Combine(Path.GetTempPath(), $"squad-missing-{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable("SQUAD_HOME", missingDir);

        var manifest = PresetLoader.LoadPreset("any");

        Assert.Null(manifest);
    }

    [Fact]
    public void ApplyPreset_InstallsAgentsIntoTargetDir()
    {
        SetupSquadHome();
        WritePreset("starter", agentNames: ["lead", "reviewer"]);

        var targetDir = Path.Combine(_tempDir, "project", "agents");
        Directory.CreateDirectory(targetDir);

        var results = PresetLoader.ApplyPreset("starter", targetDir);

        Assert.Equal(2, results.Length);
        Assert.All(results, r => Assert.Equal(PresetApplyStatus.Installed, r.Status));
        Assert.True(Directory.Exists(Path.Combine(targetDir, "lead")));
        Assert.True(Directory.Exists(Path.Combine(targetDir, "reviewer")));
    }

    [Fact]
    public void ApplyPreset_ExistingAgent_SkipsWithoutForce()
    {
        SetupSquadHome();
        WritePreset("starter", agentNames: ["lead"]);

        var targetDir = Path.Combine(_tempDir, "project", "agents");
        Directory.CreateDirectory(Path.Combine(targetDir, "lead"));

        var results = PresetLoader.ApplyPreset("starter", targetDir);

        Assert.Single(results);
        Assert.Equal(PresetApplyStatus.Skipped, results[0].Status);
    }

    [Fact]
    public void ApplyPreset_ExistingAgent_WithForce_Overwrites()
    {
        SetupSquadHome();
        WritePreset("starter", agentNames: ["lead"]);

        var targetDir = Path.Combine(_tempDir, "project", "agents");
        var existingAgentDir = Path.Combine(targetDir, "lead");
        Directory.CreateDirectory(existingAgentDir);
        File.WriteAllText(Path.Combine(existingAgentDir, "old.txt"), "old content");

        var results = PresetLoader.ApplyPreset("starter", targetDir, force: true);

        Assert.Single(results);
        Assert.Equal(PresetApplyStatus.Installed, results[0].Status);
        Assert.False(File.Exists(Path.Combine(existingAgentDir, "old.txt")));
    }

    [Fact]
    public void ApplyPreset_NonExistentPreset_ReturnsError()
    {
        SetupSquadHome();

        var targetDir = Path.Combine(_tempDir, "project", "agents");
        Directory.CreateDirectory(targetDir);

        var results = PresetLoader.ApplyPreset("doesnotexist", targetDir);

        Assert.Single(results);
        Assert.Equal(PresetApplyStatus.Error, results[0].Status);
    }

    [Fact]
    public void ApplyPreset_NoSquadHome_ReturnsError()
    {
        var missingDir = Path.Combine(Path.GetTempPath(), $"squad-missing-{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable("SQUAD_HOME", missingDir);

        var targetDir = Path.Combine(_tempDir, "project", "agents");

        var results = PresetLoader.ApplyPreset("starter", targetDir);

        Assert.Single(results);
        Assert.Equal(PresetApplyStatus.Error, results[0].Status);
    }

    [Theory]
    [InlineData("../evil")]
    [InlineData("sub/path")]
    [InlineData("..")]
    [InlineData(".")]
    public void ApplyPreset_InvalidPresetName_ReturnsError(string invalidName)
    {
        SetupSquadHome();
        var targetDir = Path.Combine(_tempDir, "project", "agents");

        var results = PresetLoader.ApplyPreset(invalidName, targetDir);

        Assert.Single(results);
        Assert.Equal(PresetApplyStatus.Error, results[0].Status);
    }

    [Fact]
    public void SavePreset_CreatesPresetFromSquadAgentsDir()
    {
        SetupSquadHome();

        var squadDir = Path.Combine(_tempDir, "myproject", ".squad");
        var agentsDir = Path.Combine(squadDir, "agents", "lead");
        Directory.CreateDirectory(agentsDir);
        File.WriteAllText(Path.Combine(agentsDir, "charter.md"), "# lead — Technical Lead\n");

        var destPath = PresetLoader.SavePreset("saved-preset", squadDir);

        Assert.True(Directory.Exists(destPath));
        Assert.True(File.Exists(Path.Combine(destPath, "preset.json")));
        Assert.True(File.Exists(Path.Combine(destPath, "agents", "lead", "charter.md")));
    }

    [Fact]
    public void SavePreset_ExistingPreset_WithoutForce_Throws()
    {
        SetupSquadHome();
        WritePreset("existing");

        var squadDir = Path.Combine(_tempDir, "myproject", ".squad");
        var agentsDir = Path.Combine(squadDir, "agents", "lead");
        Directory.CreateDirectory(agentsDir);
        File.WriteAllText(Path.Combine(agentsDir, "charter.md"), "# lead\n");

        // existing preset is at _tempDir/presets/existing — but SavePreset
        // targets _tempDir (squad home), so we need to manually create the clash
        var existingDest = Path.Combine(_tempDir, "presets", "existing");
        Directory.CreateDirectory(existingDest);

        Assert.Throws<InvalidOperationException>(() =>
            PresetLoader.SavePreset("existing", squadDir));
    }

    [Fact]
    public void SavePreset_ExistingPreset_WithForce_Overwrites()
    {
        SetupSquadHome();

        var squadDir = Path.Combine(_tempDir, "myproject", ".squad");
        var agentsDir = Path.Combine(squadDir, "agents", "lead");
        Directory.CreateDirectory(agentsDir);
        File.WriteAllText(Path.Combine(agentsDir, "charter.md"), "# lead\n");

        // Create an existing preset that should be overwritten
        var existingDest = Path.Combine(_tempDir, "presets", "mypreset");
        Directory.CreateDirectory(existingDest);
        File.WriteAllText(Path.Combine(existingDest, "old.json"), "{}");

        var destPath = PresetLoader.SavePreset("mypreset", squadDir, force: true);

        Assert.True(File.Exists(Path.Combine(destPath, "preset.json")));
    }

    [Fact]
    public void SavePreset_NoAgentsDir_Throws()
    {
        SetupSquadHome();

        var squadDir = Path.Combine(_tempDir, "myproject", ".squad");
        Directory.CreateDirectory(squadDir);

        Assert.Throws<InvalidOperationException>(() =>
            PresetLoader.SavePreset("mypreset", squadDir));
    }

    [Theory]
    [InlineData("../evil")]
    [InlineData("sub/path")]
    [InlineData("..")]
    public void SavePreset_InvalidName_ThrowsArgumentException(string invalidName)
    {
        SetupSquadHome();

        Assert.Throws<ArgumentException>(() =>
            PresetLoader.SavePreset(invalidName, _tempDir));
    }

    [Fact]
    public void InstallPreset_CopiesSourceIntoSquadHome()
    {
        SetupSquadHome();

        var sourceDir = Path.Combine(_tempDir, "external-preset");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "preset.json"), """
            {"name":"external","version":"1.0.0","description":"Test","agents":[{"name":"lead","role":"lead"}]}
            """);

        var installedPath = PresetLoader.InstallPreset(sourceDir, "external");

        Assert.True(Directory.Exists(installedPath));
        Assert.True(File.Exists(Path.Combine(installedPath, "preset.json")));
    }

    [Fact]
    public void SeedBuiltinPresets_InstallsDefaultPreset()
    {
        SetupSquadHome();

        var seeded = PresetLoader.SeedBuiltinPresets();

        Assert.Contains("default", seeded);
        var defaultDir = Path.Combine(_tempDir, "presets", "default");
        Assert.True(Directory.Exists(defaultDir));
        Assert.True(File.Exists(Path.Combine(defaultDir, "preset.json")));
    }

    [Fact]
    public void SeedBuiltinPresets_DefaultPreset_HasExpectedAgents()
    {
        SetupSquadHome();
        PresetLoader.SeedBuiltinPresets();

        var manifest = PresetLoader.LoadPreset("default");

        Assert.NotNull(manifest);
        var agentNames = manifest!.Agents.Select(a => a.Name).ToArray();
        Assert.Contains("lead", agentNames);
        Assert.Contains("reviewer", agentNames);
        Assert.Contains("devrel", agentNames);
        Assert.Contains("security", agentNames);
        Assert.Contains("docs", agentNames);
    }

    [Fact]
    public void SeedBuiltinPresets_DefaultPreset_AgentChartersExist()
    {
        SetupSquadHome();
        PresetLoader.SeedBuiltinPresets();

        var defaultDir = Path.Combine(_tempDir, "presets", "default");
        foreach (var agentName in new[] { "lead", "reviewer", "devrel", "security", "docs" })
        {
            var charterPath = Path.Combine(defaultDir, "agents", agentName, "charter.md");
            Assert.True(File.Exists(charterPath), $"charter.md missing for agent '{agentName}'");
        }
    }

    [Fact]
    public void SeedBuiltinPresets_AlreadySeeded_DoesNotOverwrite()
    {
        SetupSquadHome();
        PresetLoader.SeedBuiltinPresets();

        // Modify the installed preset
        var defaultDir = Path.Combine(_tempDir, "presets", "default");
        var markerFile = Path.Combine(defaultDir, "user-customization.txt");
        File.WriteAllText(markerFile, "user customization");

        // Seed again — should not overwrite
        PresetLoader.SeedBuiltinPresets();

        Assert.True(File.Exists(markerFile));
    }

    [Fact]
    public void SeedBuiltinPresets_AlreadySeeded_ReturnsEmptyArray()
    {
        SetupSquadHome();
        PresetLoader.SeedBuiltinPresets();

        var secondSeed = PresetLoader.SeedBuiltinPresets();

        Assert.Empty(secondSeed);
    }

    [Fact]
    public void ApplyPreset_AfterSeed_InstallsDefaultAgents()
    {
        SetupSquadHome();
        PresetLoader.SeedBuiltinPresets();

        var targetDir = Path.Combine(_tempDir, "project", "agents");
        Directory.CreateDirectory(targetDir);

        var results = PresetLoader.ApplyPreset("default", targetDir);

        Assert.All(results, r => Assert.Equal(PresetApplyStatus.Installed, r.Status));
        Assert.Equal(5, results.Length);
    }
}

#endregion
