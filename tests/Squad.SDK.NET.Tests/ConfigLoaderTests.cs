using System.Text.Json;
using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Tests;

public sealed class ConfigLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public ConfigLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"squad-configloader-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string WriteTempJson(string json, string fileName = "config.json")
    {
        var path = Path.Combine(_tempDir, fileName);
        File.WriteAllText(path, json);
        return path;
    }

    private static string MinimalValidJson => """
        {
            "team": { "name": "TestTeam" },
            "agents": [
                { "name": "agent1", "role": "Backend", "prompt": "do work" }
            ]
        }
        """;

    #region LoadAsync

    [Fact]
    public async Task LoadAsync_ValidJsonFile_DeserializesSquadConfig()
    {
        // Arrange
        var path = WriteTempJson(MinimalValidJson);

        // Act
        var config = await ConfigLoader.LoadAsync(path);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("TestTeam", config.Team.Name);
        Assert.Single(config.Agents);
        Assert.Equal("agent1", config.Agents[0].Name);
        Assert.Equal("Backend", config.Agents[0].Role);
    }

    [Fact]
    public async Task LoadAsync_FullConfig_DeserializesAllProperties()
    {
        // Arrange
        var json = """
            {
                "version": "2.0",
                "team": { "name": "FullTeam", "description": "A test team", "defaultTier": "Premium" },
                "agents": [
                    {
                        "name": "coder",
                        "role": "Developer",
                        "prompt": "write code",
                        "expertise": ["C#", "Python"],
                        "status": "Active"
                    }
                ],
                "routing": {
                    "rules": [
                        { "workType": "backend", "agents": ["coder"], "priority": 5 }
                    ],
                    "defaultAgent": "coder"
                }
            }
            """;
        var path = WriteTempJson(json);

        // Act
        var config = await ConfigLoader.LoadAsync(path);

        // Assert
        Assert.Equal("2.0", config.Version);
        Assert.Equal("FullTeam", config.Team.Name);
        Assert.Equal("A test team", config.Team.Description);
        Assert.Equal(ModelTier.Premium, config.Team.DefaultTier);
        Assert.NotNull(config.Routing);
        Assert.Single(config.Routing.Rules);
        Assert.Equal("backend", config.Routing.Rules[0].WorkType);
        Assert.Equal(5, config.Routing.Rules[0].Priority);
        Assert.Equal("coder", config.Routing.DefaultAgent);
    }

    [Fact]
    public async Task LoadAsync_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var path = WriteTempJson("{ this is not valid json }");

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() => ConfigLoader.LoadAsync(path));
    }

    [Fact]
    public async Task LoadAsync_NullJsonLiteral_ThrowsInvalidOperationException()
    {
        // Arrange — "null" is valid JSON but deserializes to null
        var path = WriteTempJson("null");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => ConfigLoader.LoadAsync(path));
    }

    [Fact]
    public async Task LoadAsync_NonexistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "does-not-exist.json");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => ConfigLoader.LoadAsync(path));
    }

    [Fact]
    public async Task LoadAsync_CaseInsensitivePropertyNames_Deserializes()
    {
        // Arrange — verify the source-gen context has PropertyNameCaseInsensitive = true
        var json = """
            {
                "Team": { "Name": "PascalTeam" },
                "Agents": []
            }
            """;
        var path = WriteTempJson(json);

        // Act
        var config = await ConfigLoader.LoadAsync(path);

        // Assert
        Assert.Equal("PascalTeam", config.Team.Name);
    }

    [Fact]
    public async Task LoadAsync_SupportsCancellation()
    {
        // Arrange
        var path = WriteTempJson(MinimalValidJson);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ConfigLoader.LoadAsync(path, cts.Token));
    }

    #endregion

    #region LoadSync

    [Fact]
    public void LoadSync_ValidJsonFile_DeserializesSquadConfig()
    {
        // Arrange
        var path = WriteTempJson(MinimalValidJson);

        // Act
        var config = ConfigLoader.LoadSync(path);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("TestTeam", config.Team.Name);
        Assert.Single(config.Agents);
        Assert.Equal("agent1", config.Agents[0].Name);
    }

    [Fact]
    public void LoadSync_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var path = WriteTempJson("not json at all");

        // Act & Assert
        Assert.Throws<JsonException>(() => ConfigLoader.LoadSync(path));
    }

    [Fact]
    public void LoadSync_NullJsonLiteral_ThrowsInvalidOperationException()
    {
        // Arrange
        var path = WriteTempJson("null");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ConfigLoader.LoadSync(path));
    }

    [Fact]
    public void LoadSync_NonexistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "no-file.json");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => ConfigLoader.LoadSync(path));
    }

    #endregion

    #region Round-trip (verifies source-gen context serialization)

    [Fact]
    public void RoundTrip_SerializeThenDeserialize_PreservesConfig()
    {
        // Arrange — build a config, serialize it using the public System.Text.Json API,
        // then load it back through ConfigLoader which uses the source-gen context.
        var original = new SquadConfig
        {
            Version = "1.5",
            Team = new TeamConfig { Name = "RoundTrip", Description = "round-trip test" },
            Agents =
            [
                new AgentConfig { Name = "a1", Role = "Tester", Prompt = "test things" }
            ]
        };

        // Serialize with default options (camelCase property names are fine because
        // the source-gen context has PropertyNameCaseInsensitive = true).
        var json = JsonSerializer.Serialize(original);
        var path = WriteTempJson(json);

        // Act
        var loaded = ConfigLoader.LoadSync(path);

        // Assert
        Assert.Equal(original.Version, loaded.Version);
        Assert.Equal(original.Team.Name, loaded.Team.Name);
        Assert.Equal(original.Team.Description, loaded.Team.Description);
        Assert.Equal(original.Agents.Count, loaded.Agents.Count);
        Assert.Equal(original.Agents[0].Name, loaded.Agents[0].Name);
        Assert.Equal(original.Agents[0].Role, loaded.Agents[0].Role);
    }

    [Fact]
    public async Task RoundTrip_Async_SerializeThenDeserialize_PreservesConfig()
    {
        // Arrange
        var original = new SquadConfig
        {
            Team = new TeamConfig { Name = "AsyncRT" },
            Agents =
            [
                new AgentConfig { Name = "b1", Role = "Dev", Prompt = "code" }
            ]
        };
        var json = JsonSerializer.Serialize(original);
        var path = WriteTempJson(json);

        // Act
        var loaded = await ConfigLoader.LoadAsync(path);

        // Assert
        Assert.Equal(original.Team.Name, loaded.Team.Name);
        Assert.Equal(original.Agents[0].Name, loaded.Agents[0].Name);
    }

    #endregion

    #region StringEnumConverter (verifies source-gen UseStringEnumConverter option)

    [Fact]
    public void LoadSync_StringEnumValues_DeserializesCorrectly()
    {
        // Arrange — enum values as strings, testing UseStringEnumConverter in source-gen context
        var json = """
            {
                "team": { "name": "EnumTeam", "defaultTier": "Fast" },
                "agents": [
                    { "name": "e1", "role": "QA", "prompt": "test", "status": "Inactive" }
                ]
            }
            """;
        var path = WriteTempJson(json);

        // Act
        var config = ConfigLoader.LoadSync(path);

        // Assert
        Assert.Equal(ModelTier.Fast, config.Team.DefaultTier);
        Assert.Equal(AgentStatus.Inactive, config.Agents[0].Status);
    }

    #endregion

    #region Validate integration (loaded config flows through validation)

    [Fact]
    public async Task LoadAsync_ThenValidate_ValidConfig_NoErrors()
    {
        // Arrange
        var path = WriteTempJson(MinimalValidJson);

        // Act
        var config = await ConfigLoader.LoadAsync(path);
        var errors = ConfigLoader.Validate(config);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void LoadSync_ThenValidate_MissingTeamName_ReturnsError()
    {
        // Arrange
        var json = """
            {
                "team": { "name": "" },
                "agents": []
            }
            """;
        var path = WriteTempJson(json);

        // Act
        var config = ConfigLoader.LoadSync(path);
        var errors = ConfigLoader.Validate(config);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Team.Name"));
    }

    #endregion
}
