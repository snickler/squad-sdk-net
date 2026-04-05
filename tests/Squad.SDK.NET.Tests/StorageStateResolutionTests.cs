using Microsoft.Extensions.Logging.Abstractions;
using Squad.SDK.NET.Resolution;
using Squad.SDK.NET.State;
using Squad.SDK.NET.Storage;

namespace Squad.SDK.NET.Tests;

#region Storage — InMemoryStorageProvider

public sealed class InMemoryStorageProviderTests
{
    private readonly InMemoryStorageProvider _sut = new();

    [Fact]
    public async Task WriteAsync_ThenReadAsync_ReturnsStoredValue()
    {
        await _sut.WriteAsync("key1", "value1");

        var result = await _sut.ReadAsync("key1");

        Assert.Equal("value1", result);
    }

    [Fact]
    public async Task ReadAsync_MissingKey_ReturnsNull()
    {
        var result = await _sut.ReadAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_ExistingKey_ReturnsTrue()
    {
        await _sut.WriteAsync("key1", "value1");

        Assert.True(await _sut.ExistsAsync("key1"));
    }

    [Fact]
    public async Task ExistsAsync_MissingKey_ReturnsFalse()
    {
        Assert.False(await _sut.ExistsAsync("nonexistent"));
    }

    [Fact]
    public async Task DeleteAsync_ExistingKey_RemovesEntry()
    {
        await _sut.WriteAsync("key1", "value1");

        await _sut.DeleteAsync("key1");

        Assert.Null(await _sut.ReadAsync("key1"));
    }

    [Fact]
    public async Task DeleteAsync_MissingKey_DoesNotThrow()
    {
        var exception = await Record.ExceptionAsync(() => _sut.DeleteAsync("nonexistent"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task ListAsync_NoPrefix_ReturnsAllKeysSorted()
    {
        await _sut.WriteAsync("c", "3");
        await _sut.WriteAsync("a", "1");
        await _sut.WriteAsync("b", "2");

        var keys = await _sut.ListAsync();

        Assert.Equal(3, keys.Count);
        Assert.Equal(["a", "b", "c"], keys);
    }

    [Fact]
    public async Task ListAsync_WithPrefix_FiltersKeys()
    {
        await _sut.WriteAsync("agents/a1.json", "{}");
        await _sut.WriteAsync("agents/a2.json", "{}");
        await _sut.WriteAsync("logs/l1.json", "{}");

        var keys = await _sut.ListAsync("agents/");

        Assert.Equal(2, keys.Count);
        Assert.All(keys, k => Assert.StartsWith("agents/", k));
    }

    [Fact]
    public async Task ListAsync_NonMatchingPrefix_ReturnsEmpty()
    {
        await _sut.WriteAsync("agents/a1.json", "{}");

        var keys = await _sut.ListAsync("decisions/");

        Assert.Empty(keys);
    }

    [Fact]
    public async Task GetStatsAsync_PopulatedStore_ReturnsCorrectCounts()
    {
        await _sut.WriteAsync("k1", "hello");
        await _sut.WriteAsync("k2", "world!");

        var stats = await _sut.GetStatsAsync();

        Assert.Equal(2, stats.ItemCount);
        Assert.True(stats.TotalSizeBytes > 0);
        Assert.NotNull(stats.LastModified);
    }

    [Fact]
    public async Task GetStatsAsync_EmptyStore_ReturnsZeroCounts()
    {
        var stats = await _sut.GetStatsAsync();

        Assert.Equal(0, stats.ItemCount);
        Assert.Equal(0L, stats.TotalSizeBytes);
        Assert.Null(stats.LastModified);
    }

    [Fact]
    public async Task WriteAsync_ConcurrentWrites_AllSucceed()
    {
        var tasks = Enumerable.Range(0, 50)
            .Select(i => _sut.WriteAsync($"key-{i}", $"value-{i}"));

        await Task.WhenAll(tasks);

        var keys = await _sut.ListAsync();
        Assert.Equal(50, keys.Count);
    }

    [Fact]
    public async Task WriteAsync_OverwritesExistingValue()
    {
        await _sut.WriteAsync("key1", "original");
        await _sut.WriteAsync("key1", "updated");

        var result = await _sut.ReadAsync("key1");

        Assert.Equal("updated", result);
    }
}

#endregion

#region Storage — FileSystemStorageProvider

public sealed class FileSystemStorageProviderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileSystemStorageProvider _sut;

    public FileSystemStorageProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"squad-test-{Guid.NewGuid():N}");
        _sut = new FileSystemStorageProvider(_tempDir, NullLogger<FileSystemStorageProvider>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task WriteAsync_ThenReadAsync_RoundTrip()
    {
        await _sut.WriteAsync("file1.txt", "content1");

        var result = await _sut.ReadAsync("file1.txt");

        Assert.Equal("content1", result);
    }

    [Fact]
    public async Task ReadAsync_MissingKey_ReturnsNull()
    {
        var result = await _sut.ReadAsync("missing.txt");

        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_ExistingKey_ReturnsTrue()
    {
        await _sut.WriteAsync("file.txt", "data");

        Assert.True(await _sut.ExistsAsync("file.txt"));
    }

    [Fact]
    public async Task ExistsAsync_MissingKey_ReturnsFalse()
    {
        Assert.False(await _sut.ExistsAsync("nope.txt"));
    }

    [Fact]
    public async Task DeleteAsync_ExistingKey_RemovesFile()
    {
        await _sut.WriteAsync("file.txt", "data");

        await _sut.DeleteAsync("file.txt");

        Assert.Null(await _sut.ReadAsync("file.txt"));
    }

    [Fact]
    public async Task WriteAsync_NestedPath_CreatesDirectories()
    {
        await _sut.WriteAsync("sub/dir/file.txt", "nested");

        var result = await _sut.ReadAsync("sub/dir/file.txt");

        Assert.Equal("nested", result);
    }

    [Fact]
    public async Task ListAsync_NoPrefix_ReturnsAllFiles()
    {
        await _sut.WriteAsync("a.json", "{}");
        await _sut.WriteAsync("b.json", "{}");

        var keys = await _sut.ListAsync();

        Assert.Equal(2, keys.Count);
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsCorrectStats()
    {
        await _sut.WriteAsync("f1.txt", "hello");
        await _sut.WriteAsync("f2.txt", "world!");

        var stats = await _sut.GetStatsAsync();

        Assert.Equal(2, stats.ItemCount);
        Assert.True(stats.TotalSizeBytes > 0);
        Assert.NotNull(stats.LastModified);
    }

    [Fact]
    public async Task GetStatsAsync_EmptyDir_ReturnsZeroCounts()
    {
        var stats = await _sut.GetStatsAsync();

        Assert.Equal(0, stats.ItemCount);
        Assert.Equal(0L, stats.TotalSizeBytes);
    }

    [Fact]
    public async Task Constructor_CreatesRootDirectory()
    {
        var customDir = Path.Combine(Path.GetTempPath(), $"squad-ctor-{Guid.NewGuid():N}");
        try
        {
            _ = new FileSystemStorageProvider(customDir, NullLogger<FileSystemStorageProvider>.Instance);

            Assert.True(Directory.Exists(customDir));
        }
        finally
        {
            if (Directory.Exists(customDir))
                Directory.Delete(customDir, recursive: true);
        }
    }
}

#endregion

#region Storage — StorageErrors

public sealed class StorageErrorTests
{
    [Fact]
    public void StorageError_MessageAndKey_AreSet()
    {
        var error = new StorageError("fail", "mykey");

        Assert.Equal("fail", error.Message);
        Assert.Equal("mykey", error.Key);
    }

    [Fact]
    public void StorageError_NullKey_IsAllowed()
    {
        var error = new StorageError("fail");

        Assert.Null(error.Key);
    }

    [Fact]
    public void ProviderError_InnerException_Propagated()
    {
        var inner = new InvalidOperationException("inner");

        var error = new ProviderError("provider fail", "k", inner);

        Assert.Equal("provider fail", error.Message);
        Assert.Equal("k", error.Key);
        Assert.Same(inner, error.InnerException);
    }

    [Fact]
    public void StorageFullError_Properties_Set()
    {
        var error = new StorageFullError("disk full", "bigkey");

        Assert.Equal("disk full", error.Message);
        Assert.Equal("bigkey", error.Key);
    }

    [Fact]
    public void AccessDeniedError_Properties_Set()
    {
        var error = new AccessDeniedError("no access", "secret");

        Assert.Equal("no access", error.Message);
        Assert.Equal("secret", error.Key);
    }

    [Theory]
    [InlineData(typeof(ProviderError))]
    [InlineData(typeof(StorageFullError))]
    [InlineData(typeof(AccessDeniedError))]
    public void StorageErrorSubtype_DeriveFromStorageError(Type errorType)
    {
        Assert.True(typeof(StorageError).IsAssignableFrom(errorType));
    }

    [Fact]
    public void StorageError_WithInnerException_Propagated()
    {
        var inner = new IOException("io");

        var error = new StorageError("wrapped", "k1", inner);

        Assert.Same(inner, error.InnerException);
    }
}

#endregion

#region State — DomainTypes

public sealed class DomainTypeTests
{
    [Fact]
    public void AgentEntity_RequiredProperties_Populated()
    {
        var agent = new AgentEntity { Name = "TestAgent" };

        Assert.Equal("TestAgent", agent.Name);
        Assert.Null(agent.Role);
        Assert.Null(agent.Description);
        Assert.Null(agent.Status);
        Assert.Null(agent.Model);
        Assert.Empty(agent.Expertise);
        Assert.Null(agent.LastActiveAt);
    }

    [Fact]
    public void AgentEntity_AllProperties_Set()
    {
        var now = DateTimeOffset.UtcNow;
        var agent = new AgentEntity
        {
            Name = "Bot",
            Role = "coder",
            Description = "A coding bot",
            Status = "active",
            Model = "gpt-4",
            Expertise = ["C#", "Python"],
            CreatedAt = now,
            LastActiveAt = now
        };

        Assert.Equal("Bot", agent.Name);
        Assert.Equal("coder", agent.Role);
        Assert.Equal(2, agent.Expertise.Count);
        Assert.Equal(now, agent.LastActiveAt);
    }

    [Fact]
    public void Decision_DefaultStatus_IsProposed()
    {
        var decision = new Decision { Id = "d1", Title = "T", Description = "D" };

        Assert.Equal(DecisionStatus.Proposed, decision.Status);
        Assert.Empty(decision.Tags);
    }

    [Theory]
    [InlineData(DecisionStatus.Proposed)]
    [InlineData(DecisionStatus.Accepted)]
    [InlineData(DecisionStatus.Rejected)]
    [InlineData(DecisionStatus.Superseded)]
    public void DecisionStatus_Value_IsDefined(DecisionStatus status)
    {
        Assert.True(Enum.IsDefined(status));
    }

    [Fact]
    public void HistoryEntry_RequiredProperties_Set()
    {
        var entry = new HistoryEntry { Id = "h1", AgentName = "Agent1", Action = "build" };

        Assert.Equal("h1", entry.Id);
        Assert.Equal("Agent1", entry.AgentName);
        Assert.Equal("build", entry.Action);
        Assert.Null(entry.Details);
    }

    [Fact]
    public void TeamMember_RequiredProperties_Set()
    {
        var member = new TeamMember { Name = "Alice", Role = "lead" };

        Assert.Equal("Alice", member.Name);
        Assert.Equal("lead", member.Role);
        Assert.False(member.IsLead);
        Assert.Null(member.Description);
    }

    [Fact]
    public void Template_RequiredAndDefaultProperties_Set()
    {
        var template = new Template { Id = "t1", Name = "Default", Content = "body" };

        Assert.Equal("t1", template.Id);
        Assert.Equal("Default", template.Name);
        Assert.Equal("body", template.Content);
        Assert.Null(template.Category);
        Assert.Empty(template.Tags);
    }

    [Fact]
    public void LogEntry_RequiredAndDefaultProperties_Set()
    {
        var log = new LogEntry { Id = "l1", Level = "Info", Message = "hello" };

        Assert.Equal("l1", log.Id);
        Assert.Equal("Info", log.Level);
        Assert.Equal("hello", log.Message);
        Assert.Null(log.AgentName);
        Assert.Null(log.SessionId);
        Assert.Null(log.Metadata);
    }

    [Fact]
    public void LogEntry_WithMetadata_Accessible()
    {
        var meta = new Dictionary<string, string> { ["env"] = "test" };
        var log = new LogEntry
        {
            Id = "l2",
            Level = "Debug",
            Message = "with meta",
            Metadata = meta
        };

        Assert.NotNull(log.Metadata);
        Assert.Equal("test", log.Metadata["env"]);
    }
}

#endregion

#region State — StateErrors

public sealed class StateErrorTests
{
    [Fact]
    public void StateError_Message_Propagated()
    {
        var error = new StateError("bad state");

        Assert.Equal("bad state", error.Message);
    }

    [Fact]
    public void StateError_InnerException_Propagated()
    {
        var inner = new Exception("root cause");

        var error = new StateError("wrapped", inner);

        Assert.Same(inner, error.InnerException);
    }

    [Fact]
    public void NotFoundError_Properties_Set()
    {
        var error = new NotFoundError("Agent", "a1");

        Assert.Equal("Agent", error.EntityType);
        Assert.Equal("a1", error.Key);
        Assert.Contains("Agent", error.Message);
        Assert.Contains("a1", error.Message);
    }

    [Fact]
    public void ParseError_FilePath_Propagated()
    {
        var inner = new FormatException("bad json");

        var error = new ParseError("parse failed", "/path/file.json", inner);

        Assert.Equal("parse failed", error.Message);
        Assert.Equal("/path/file.json", error.FilePath);
        Assert.Same(inner, error.InnerException);
    }

    [Fact]
    public void ParseError_NullFilePath_Allowed()
    {
        var error = new ParseError("parse failed");

        Assert.Null(error.FilePath);
    }

    [Fact]
    public void WriteConflictError_Key_Propagated()
    {
        var error = new WriteConflictError("agents/a1");

        Assert.Equal("agents/a1", error.Key);
        Assert.Contains("agents/a1", error.Message);
    }

    [Theory]
    [InlineData(typeof(NotFoundError))]
    [InlineData(typeof(ParseError))]
    [InlineData(typeof(WriteConflictError))]
    public void StateErrorSubtype_DeriveFromStateError(Type errorType)
    {
        Assert.True(typeof(StateError).IsAssignableFrom(errorType));
    }
}

#endregion

#region State — SquadState with InMemoryStorageProvider

public sealed class SquadStateTests
{
    private readonly InMemoryStorageProvider _storage = new();
    private readonly SquadState _sut;

    public SquadStateTests()
    {
        _sut = new SquadState(_storage);
    }

    [Fact]
    public async Task Agents_SetAsync_GetAsync_RoundTrip()
    {
        var agent = new AgentEntity { Name = "Bot1", Role = "coder" };

        await _sut.Agents.SetAsync("bot1", agent);
        var result = await _sut.Agents.GetAsync("bot1");

        Assert.NotNull(result);
        Assert.Equal("Bot1", result.Name);
        Assert.Equal("coder", result.Role);
    }

    [Fact]
    public async Task Agents_GetAsync_MissingKey_ReturnsNull()
    {
        var result = await _sut.Agents.GetAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task Decisions_SetAsync_ListKeysAsync_ReturnsAllKeys()
    {
        var d1 = new Decision { Id = "d1", Title = "Use X", Description = "We choose X" };
        var d2 = new Decision { Id = "d2", Title = "Use Y", Description = "We choose Y" };
        await _sut.Decisions.SetAsync("d1", d1);
        await _sut.Decisions.SetAsync("d2", d2);

        var keys = await _sut.Decisions.ListKeysAsync();

        Assert.Equal(2, keys.Count);
        Assert.Contains("d1", keys);
        Assert.Contains("d2", keys);
    }

    [Fact]
    public async Task Agents_DeleteAsync_RemovesItem()
    {
        var agent = new AgentEntity { Name = "Temp" };
        await _sut.Agents.SetAsync("tmp", agent);

        await _sut.Agents.DeleteAsync("tmp");

        Assert.False(await _sut.Agents.ExistsAsync("tmp"));
    }

    [Fact]
    public async Task Agents_ExistsAsync_AfterSet_ReturnsTrue()
    {
        await _sut.Agents.SetAsync("x", new AgentEntity { Name = "X" });

        Assert.True(await _sut.Agents.ExistsAsync("x"));
    }

    [Fact]
    public async Task Agents_ExistsAsync_MissingKey_ReturnsFalse()
    {
        Assert.False(await _sut.Agents.ExistsAsync("nope"));
    }

    [Fact]
    public async Task History_SetAsync_GetAsync_PreservesAllFields()
    {
        var entry = new HistoryEntry
        {
            Id = "h1",
            AgentName = "Bot",
            Action = "test",
            Details = "ran unit tests"
        };

        await _sut.History.SetAsync("h1", entry);
        var result = await _sut.History.GetAsync("h1");

        Assert.NotNull(result);
        Assert.Equal("h1", result.Id);
        Assert.Equal("Bot", result.AgentName);
        Assert.Equal("test", result.Action);
        Assert.Equal("ran unit tests", result.Details);
    }

    [Fact]
    public async Task Logs_SetAsync_GetAsync_JsonRoundTrip()
    {
        var log = new LogEntry
        {
            Id = "l1",
            Level = "Warning",
            Message = "something happened",
            AgentName = "Monitor"
        };

        await _sut.Logs.SetAsync("l1", log);
        var result = await _sut.Logs.GetAsync("l1");

        Assert.NotNull(result);
        Assert.Equal("l1", result.Id);
        Assert.Equal("Warning", result.Level);
        Assert.Equal("Monitor", result.AgentName);
    }

    [Fact]
    public async Task Decisions_SetAsync_PreservesEnumStatus()
    {
        var decision = new Decision
        {
            Id = "d1",
            Title = "Accept RFC",
            Description = "We accept the RFC",
            Status = DecisionStatus.Accepted
        };

        await _sut.Decisions.SetAsync("d1", decision);
        var result = await _sut.Decisions.GetAsync("d1");

        Assert.NotNull(result);
        Assert.Equal(DecisionStatus.Accepted, result.Status);
    }
}

#endregion

#region Resolution — Types & Enums

public sealed class ResolutionTypeTests
{
    [Theory]
    [InlineData(SquadMode.Project)]
    [InlineData(SquadMode.Personal)]
    [InlineData(SquadMode.Global)]
    public void SquadMode_Value_IsDefined(SquadMode mode)
    {
        Assert.True(Enum.IsDefined(mode));
    }

    [Fact]
    public void ResolvedSquadPaths_RequiredAndOptionalProperties_Set()
    {
        var paths = new ResolvedSquadPaths
        {
            Mode = SquadMode.Project,
            ProjectDir = "/proj/.squad"
        };

        Assert.Equal(SquadMode.Project, paths.Mode);
        Assert.Equal("/proj/.squad", paths.ProjectDir);
        Assert.Null(paths.TeamDir);
        Assert.Null(paths.PersonalDir);
        Assert.Null(paths.Name);
        Assert.False(paths.IsLegacy);
    }

    [Fact]
    public void ResolvedSquadPaths_AllProperties_Set()
    {
        var paths = new ResolvedSquadPaths
        {
            Mode = SquadMode.Personal,
            ProjectDir = "/home/.config/squad",
            TeamDir = "/teams/myteam",
            PersonalDir = "/home/.config/squad",
            Name = "my-squad",
            IsLegacy = true
        };

        Assert.Equal(SquadMode.Personal, paths.Mode);
        Assert.Equal("/teams/myteam", paths.TeamDir);
        Assert.Equal("my-squad", paths.Name);
        Assert.True(paths.IsLegacy);
    }

    [Fact]
    public void SquadDirConfig_DefaultValues_Correct()
    {
        var config = new SquadDirConfig();

        Assert.Equal("1.0", config.Version);
        Assert.Null(config.TeamRoot);
        Assert.Null(config.ProjectKey);
        Assert.False(config.ExtractionDisabled);
    }

    [Fact]
    public void SquadDirConfig_CustomValues_Set()
    {
        var config = new SquadDirConfig
        {
            Version = "2.0",
            TeamRoot = "/teams",
            ProjectKey = "proj-1",
            ExtractionDisabled = true
        };

        Assert.Equal("2.0", config.Version);
        Assert.Equal("/teams", config.TeamRoot);
        Assert.Equal("proj-1", config.ProjectKey);
        Assert.True(config.ExtractionDisabled);
    }
}

#endregion

#region Resolution — SquadResolver

public sealed class SquadResolverTests
{
    [Fact]
    public void ResolveGlobalSquadPath_ReturnsNonNullPath()
    {
        var path = SquadResolver.ResolveGlobalSquadPath();

        Assert.NotNull(path);
        Assert.NotEmpty(path!);
    }

    [Fact]
    public void ResolvePersonalSquadDir_ReturnsNonNullPath()
    {
        var path = SquadResolver.ResolvePersonalSquadDir();

        Assert.NotNull(path);
        Assert.NotEmpty(path!);
    }

    [Fact]
    public void ResolveSquad_NonExistentDir_ReturnsNullOrPersonalFallback()
    {
        var fakeDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var result = SquadResolver.ResolveSquad(fakeDir);

        Assert.True(result is null || result.Mode == SquadMode.Personal);
    }

    [Fact]
    public void SquadDirName_Constant_IsExpectedValue()
    {
        Assert.Equal(".squad", SquadResolver.SquadDirName);
    }

    [Fact]
    public void ConfigFileName_Constant_IsExpectedValue()
    {
        Assert.Equal("squad.json", SquadResolver.ConfigFileName);
    }

    [Fact]
    public void ResolveSquad_DirWithSquadFolder_ReturnsProjectMode()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"squad-resolve-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(Path.Combine(tempDir, ".squad"));

            var result = SquadResolver.ResolveSquad(tempDir);

            Assert.NotNull(result);
            Assert.Equal(SquadMode.Project, result!.Mode);
            Assert.Contains(".squad", result.ProjectDir);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void IsInsideWorktree_RegularDir_ReturnsFalse()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"squad-wt-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);

            var result = SquadResolver.IsInsideWorktree(tempDir);

            Assert.False(result);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}

#endregion

#region Resolution — MultiSquadManager

public sealed class MultiSquadManagerTests : IDisposable
{
    private readonly MultiSquadManager _sut;
    private string? _createdSquadName;

    public MultiSquadManagerTests()
    {
        _sut = new MultiSquadManager(NullLogger<MultiSquadManager>.Instance);
    }

    public void Dispose()
    {
        if (_createdSquadName is not null)
        {
            try { _sut.DeleteSquad(_createdSquadName); }
            catch { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public void ListSquads_ReturnsReadOnlyList()
    {
        var squads = _sut.ListSquads();

        Assert.NotNull(squads);
        Assert.IsAssignableFrom<IReadOnlyList<string>>(squads);
    }

    [Fact]
    public void CreateSquad_EmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _sut.CreateSquad(""));
    }

    [Fact]
    public void CreateSquad_WhitespaceName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _sut.CreateSquad("   "));
    }

    [Fact]
    public void CreateSquad_ThenListSquads_ContainsNewSquad()
    {
        _createdSquadName = $"test-{Guid.NewGuid():N}";

        _sut.CreateSquad(_createdSquadName);
        var squads = _sut.ListSquads();

        Assert.Contains(_createdSquadName, squads);
    }

    [Fact]
    public void CreateSquad_DuplicateName_ThrowsInvalidOperationException()
    {
        _createdSquadName = $"test-dup-{Guid.NewGuid():N}";
        _sut.CreateSquad(_createdSquadName);

        Assert.Throws<InvalidOperationException>(() => _sut.CreateSquad(_createdSquadName));
    }

    [Fact]
    public void ResolveSquadPath_NonExistentName_ReturnsNull()
    {
        var result = _sut.ResolveSquadPath($"nonexistent-{Guid.NewGuid():N}");

        Assert.Null(result);
    }

    [Fact]
    public void DeleteSquad_NonExistentName_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _sut.DeleteSquad($"ghost-{Guid.NewGuid():N}"));
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("..\\escape")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    public void CreateSquad_PathTraversal_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() => _sut.CreateSquad(name));
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("..\\escape")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    public void DeleteSquad_PathTraversal_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() => _sut.DeleteSquad(name));
    }

    [Theory]
    [InlineData(":")]
    [InlineData("*")]
    [InlineData("?")]
    [InlineData("<")]
    [InlineData(">")]
    [InlineData("|")]
    [InlineData("\0")]
    public void CreateSquad_InvalidFileNameChars_ThrowsArgumentException(string invalidChar)
    {
        var squadName = $"squad{invalidChar}name";
        Assert.Throws<ArgumentException>(() => _sut.CreateSquad(squadName));
    }

    [Theory]
    [InlineData(":")]
    [InlineData("*")]
    [InlineData("?")]
    [InlineData("<")]
    [InlineData(">")]
    [InlineData("|")]
    [InlineData("\0")]
    public void DeleteSquad_InvalidFileNameChars_ThrowsArgumentException(string invalidChar)
    {
        var squadName = $"squad{invalidChar}name";
        Assert.Throws<ArgumentException>(() => _sut.DeleteSquad(squadName));
    }
}

#endregion
