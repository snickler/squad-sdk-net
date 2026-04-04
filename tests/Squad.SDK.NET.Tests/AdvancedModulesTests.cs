using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Agents;
using Squad.SDK.NET.Casting;
using Squad.SDK.NET.Config;
using Squad.SDK.NET.Hooks;
using Squad.SDK.NET.Marketplace;
using Squad.SDK.NET.Platform;
using Squad.SDK.NET.Remote;
using Squad.SDK.NET.Roles;
using Squad.SDK.NET.Sharing;

namespace Squad.SDK.NET.Tests;

#region Hooks — ReviewerLockoutHook

public sealed class ReviewerLockoutHookTests
{
    private readonly ReviewerLockoutHook _hook = new(NullLogger<ReviewerLockoutHook>.Instance);

    [Fact]
    public void Lockout_ThenIsLockedOut_ReturnsTrue()
    {
        // Arrange
        const string artifactId = "file.cs";
        const string agentName = "agent-1";

        // Act
        _hook.Lockout(artifactId, agentName);

        // Assert
        Assert.True(_hook.IsLockedOut(artifactId, agentName));
    }

    [Fact]
    public void IsLockedOut_NoLockout_ReturnsFalse()
    {
        Assert.False(_hook.IsLockedOut("unknown-file.cs", "agent-1"));
    }

    [Fact]
    public void IsLockedOut_DifferentAgent_ReturnsFalse()
    {
        _hook.Lockout("file.cs", "agent-1");

        Assert.False(_hook.IsLockedOut("file.cs", "agent-2"));
    }

    [Fact]
    public void ClearLockout_RemovesLockout()
    {
        // Arrange
        _hook.Lockout("file.cs", "agent-1");

        // Act
        _hook.ClearLockout("file.cs");

        // Assert
        Assert.False(_hook.IsLockedOut("file.cs", "agent-1"));
    }

    [Fact]
    public void ClearLockout_NonexistentArtifact_DoesNotThrow()
    {
        var ex = Record.Exception(() => _hook.ClearLockout("nonexistent"));
        Assert.Null(ex);
    }

    [Fact]
    public void GetLockedAgents_ReturnsCorrectMapping()
    {
        // Arrange
        _hook.Lockout("a.cs", "agent-1");
        _hook.Lockout("b.cs", "agent-2");

        // Act
        var locked = _hook.GetLockedAgents();

        // Assert
        Assert.Equal(2, locked.Count);
        Assert.Equal("agent-1", locked["a.cs"]);
        Assert.Equal("agent-2", locked["b.cs"]);
    }

    [Fact]
    public void GetLockedAgents_EmptyWhenNone()
    {
        Assert.Empty(_hook.GetLockedAgents());
    }

    [Fact]
    public void ClearAll_RemovesAllLockouts()
    {
        // Arrange
        _hook.Lockout("a.cs", "agent-1");
        _hook.Lockout("b.cs", "agent-2");

        // Act
        _hook.ClearAll();

        // Assert
        Assert.Empty(_hook.GetLockedAgents());
        Assert.False(_hook.IsLockedOut("a.cs", "agent-1"));
    }

    [Fact]
    public void Lockout_OverwritesSameArtifact()
    {
        _hook.Lockout("file.cs", "agent-1");
        _hook.Lockout("file.cs", "agent-2");

        Assert.False(_hook.IsLockedOut("file.cs", "agent-1"));
        Assert.True(_hook.IsLockedOut("file.cs", "agent-2"));
    }

    [Fact]
    public async Task CreateHook_AllowsUnlockedArtifact()
    {
        var hook = _hook.CreateHook();
        var context = new PreToolUseContext
        {
            ToolName = "write_file",
            AgentName = "agent-1",
            SessionId = "s1",
            Arguments = new Dictionary<string, object?> { ["path"] = "safe.cs" }
        };

        var result = await hook(context);

        Assert.Equal(HookAction.Allow, result.Action);
    }

    [Fact]
    public async Task CreateHook_BlocksLockedArtifact()
    {
        _hook.Lockout("locked.cs", "agent-1");
        var hook = _hook.CreateHook();
        var context = new PreToolUseContext
        {
            ToolName = "write_file",
            AgentName = "agent-1",
            SessionId = "s1",
            Arguments = new Dictionary<string, object?> { ["path"] = "locked.cs" }
        };

        var result = await hook(context);

        Assert.Equal(HookAction.Block, result.Action);
        Assert.Contains("locked.cs", result.Reason);
    }

    [Fact]
    public async Task CreateHook_NoPathArgument_Allows()
    {
        _hook.Lockout("file.cs", "agent-1");
        var hook = _hook.CreateHook();
        var context = new PreToolUseContext
        {
            ToolName = "read_file",
            AgentName = "agent-1",
            SessionId = "s1",
            Arguments = new Dictionary<string, object?>()
        };

        var result = await hook(context);

        Assert.Equal(HookAction.Allow, result.Action);
    }
}

#endregion

#region Hooks — PiiScrubberHook

public sealed class PiiScrubberHookTests
{
    [Theory]
    [InlineData("Contact test@example.com", "Contact [EMAIL REDACTED]")]
    [InlineData("Email: user.name+tag@domain.co", "Email: [EMAIL REDACTED]")]
    public void ScrubPii_ScrubsEmails(string input, string expected)
    {
        Assert.Equal(expected, PiiScrubberHook.ScrubPii(input));
    }

    [Theory]
    [InlineData("Call 555-123-4567 now", "Call [PHONE REDACTED] now")]
    [InlineData("Phone: 5551234567", "Phone: [PHONE REDACTED]")]
    public void ScrubPii_ScrubsPhoneNumbers(string input, string expected)
    {
        Assert.Equal(expected, PiiScrubberHook.ScrubPii(input));
    }

    [Theory]
    [InlineData("SSN is 123-45-6789", "SSN is [SSN REDACTED]")]
    [InlineData("SSN: 999-88-7777", "SSN: [SSN REDACTED]")]
    public void ScrubPii_ScrubsSocialSecurityNumbers(string input, string expected)
    {
        Assert.Equal(expected, PiiScrubberHook.ScrubPii(input));
    }

    [Fact]
    public void ScrubPii_NoPii_ReturnsUnchanged()
    {
        const string input = "Hello, this is just normal text.";
        Assert.Equal(input, PiiScrubberHook.ScrubPii(input));
    }

    [Fact]
    public void ScrubPii_MultiplePiiTypes_ScrubsAll()
    {
        const string input = "Email: a@b.com, SSN: 123-45-6789, Phone: 555-111-2222";
        var result = PiiScrubberHook.ScrubPii(input);

        Assert.Contains("[EMAIL REDACTED]", result);
        Assert.Contains("[SSN REDACTED]", result);
        Assert.Contains("[PHONE REDACTED]", result);
        Assert.DoesNotContain("a@b.com", result);
    }

    [Fact]
    public void ScrubPii_EmptyString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, PiiScrubberHook.ScrubPii(string.Empty));
    }

    [Fact]
    public async Task CreateHook_ScrubsPiiFromResult()
    {
        var hook = new PiiScrubberHook(NullLogger<PiiScrubberHook>.Instance);
        var hookFunc = hook.CreateHook();
        var context = new PostToolUseContext
        {
            ToolName = "bash",
            AgentName = "agent-1",
            SessionId = "s1",
            Result = "Contact admin@corp.com for help"
        };

        var result = await hookFunc(context);

        Assert.True(result.Success);
        Assert.Contains("[EMAIL REDACTED]", result.ScrubbedResult);
    }

    [Fact]
    public async Task CreateHook_NoPii_ReturnsOk()
    {
        var hook = new PiiScrubberHook(NullLogger<PiiScrubberHook>.Instance);
        var hookFunc = hook.CreateHook();
        var context = new PostToolUseContext
        {
            ToolName = "bash",
            AgentName = "agent-1",
            SessionId = "s1",
            Result = "Build succeeded"
        };

        var result = await hookFunc(context);

        Assert.True(result.Success);
        Assert.Null(result.ScrubbedResult);
    }

    [Fact]
    public async Task CreateHook_NonStringResult_ReturnsOk()
    {
        var hook = new PiiScrubberHook(NullLogger<PiiScrubberHook>.Instance);
        var hookFunc = hook.CreateHook();
        var context = new PostToolUseContext
        {
            ToolName = "bash",
            AgentName = "agent-1",
            SessionId = "s1",
            Result = 42
        };

        var result = await hookFunc(context);

        Assert.True(result.Success);
        Assert.Null(result.ScrubbedResult);
    }
}

#endregion

#region Roles — BaseRole & RoleCatalog

public sealed class RoleCatalogTests
{
    [Fact]
    public void GetAllRoles_ReturnsTenRoles()
    {
        var roles = RoleCatalog.GetAllRoles();
        Assert.Equal(10, roles.Count);
    }

    [Theory]
    [InlineData("lead", "Team Lead")]
    [InlineData("frontend", "Frontend Engineer")]
    [InlineData("backend", "Backend Engineer")]
    [InlineData("tester", "Quality Engineer")]
    [InlineData("scribe", "Technical Writer")]
    [InlineData("architect", "Software Architect")]
    [InlineData("security", "Security Engineer")]
    [InlineData("devops", "DevOps Engineer")]
    [InlineData("designer", "UX Designer")]
    [InlineData("devrel", "Developer Advocate")]
    public void GetRole_ValidId_ReturnsExpectedRole(string id, string expectedName)
    {
        var role = RoleCatalog.GetRole(id);
        Assert.NotNull(role);
        Assert.Equal(expectedName, role.Name);
        Assert.Equal(id, role.Id);
    }

    [Fact]
    public void GetRole_CaseInsensitive_ReturnsRole()
    {
        var role = RoleCatalog.GetRole("LEAD");
        Assert.NotNull(role);
        Assert.Equal("lead", role.Id);
    }

    [Fact]
    public void GetRole_Nonexistent_ReturnsNull()
    {
        Assert.Null(RoleCatalog.GetRole("nonexistent"));
    }

    [Fact]
    public void UseRole_ValidRole_ReturnsCharter()
    {
        var charter = RoleCatalog.UseRole("tester", "qa-bot");

        Assert.Equal("qa-bot", charter.Name);
        Assert.Equal("Quality Engineer", charter.DisplayName);
        Assert.Equal("tester", charter.Role);
        Assert.NotNull(charter.Prompt);
        Assert.NotEmpty(charter.Prompt);
    }

    [Fact]
    public void UseRole_Nonexistent_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => RoleCatalog.UseRole("nonexistent", "bot"));
    }

    [Fact]
    public void UseRole_WithAdditionalPrompt_AppendsToPrompt()
    {
        var charter = RoleCatalog.UseRole("lead", "lead-bot", "Focus on .NET projects.");

        Assert.Contains("Focus on .NET projects.", charter.Prompt);
    }

    [Fact]
    public void AllRoles_HaveValidCategory()
    {
        var roles = RoleCatalog.GetAllRoles();
        foreach (var role in roles)
        {
            Assert.True(Enum.IsDefined(role.Category));
        }
    }

    [Fact]
    public void AllRoles_HaveNonEmptyExpertise()
    {
        var roles = RoleCatalog.GetAllRoles();
        foreach (var role in roles)
        {
            Assert.NotEmpty(role.Expertise);
        }
    }

    [Theory]
    [InlineData(RoleCategory.Engineering)]
    [InlineData(RoleCategory.Testing)]
    [InlineData(RoleCategory.Documentation)]
    public void GetByCategory_ReturnsMatchingRoles(RoleCategory category)
    {
        var roles = RoleCatalog.GetByCategory(category);

        Assert.NotEmpty(roles);
        Assert.All(roles, r => Assert.Equal(category, r.Category));
    }

    [Fact]
    public void SearchRoles_ByExpertise_FindsMatches()
    {
        var results = RoleCatalog.SearchRoles("testing");
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Id == "tester");
    }

    [Fact]
    public void SearchRoles_NoMatch_ReturnsEmpty()
    {
        var results = RoleCatalog.SearchRoles("xyznonexistent");
        Assert.Empty(results);
    }
}

#endregion

#region Casting — CastingTypes & CastingEngine

public sealed class CastingEngineTests
{
    private readonly CastingEngine _engine = new(null, NullLogger<CastingEngine>.Instance);

    [Fact]
    public void Cast_ProducesCastMember()
    {
        var member = _engine.Cast("agent-1", "lead", "StarWars");

        Assert.NotNull(member);
        Assert.Equal("StarWars", member.Universe);
        Assert.Contains("agent-1", member.Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetAllCasts_InitiallyEmpty()
    {
        Assert.Empty(_engine.GetAllCasts());
    }

    [Fact]
    public void Cast_ThenGetCast_RoundTrip()
    {
        _engine.Cast("agent-1", "tester", "Marvel");

        var record = _engine.GetCast("agent-1");

        Assert.NotNull(record);
        Assert.Equal("agent-1", record.AgentName);
        Assert.Equal("tester", record.RoleId);
    }

    [Fact]
    public void Cast_ThenGetAllCasts_ContainsRecord()
    {
        _engine.Cast("agent-1", "lead");
        _engine.Cast("agent-2", "tester");

        var all = _engine.GetAllCasts();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void GetCast_Unknown_ReturnsNull()
    {
        Assert.Null(_engine.GetCast("nonexistent"));
    }

    [Fact]
    public void RemoveCast_RemovesAgent()
    {
        _engine.Cast("agent-1", "lead");
        _engine.RemoveCast("agent-1");

        Assert.Null(_engine.GetCast("agent-1"));
    }

    [Fact]
    public void ClearAll_EmptiesAllCasts()
    {
        _engine.Cast("a", "lead");
        _engine.Cast("b", "tester");

        _engine.ClearAll();

        Assert.Empty(_engine.GetAllCasts());
    }

    [Fact]
    public void Cast_WithCapacityReject_ThrowsOnOverflow()
    {
        var config = new CastingConfig
        {
            Capacity = 1,
            OverflowStrategy = OverflowStrategy.Reject
        };
        var engine = new CastingEngine(config, NullLogger<CastingEngine>.Instance);

        engine.Cast("agent-1", "lead");

        Assert.Throws<InvalidOperationException>(() => engine.Cast("agent-2", "tester"));
    }

    [Fact]
    public void Cast_WithCapacityRotate_EvictsOldest()
    {
        var config = new CastingConfig
        {
            Capacity = 1,
            OverflowStrategy = OverflowStrategy.Rotate
        };
        var engine = new CastingEngine(config, NullLogger<CastingEngine>.Instance);

        engine.Cast("agent-1", "lead");
        engine.Cast("agent-2", "tester");

        Assert.Null(engine.GetCast("agent-1"));
        Assert.NotNull(engine.GetCast("agent-2"));
    }

    [Fact]
    public void Cast_WithCapacityQueue_ThrowsNotSupportedException()
    {
        var config = new CastingConfig
        {
            Capacity = 1,
            OverflowStrategy = OverflowStrategy.Queue
        };
        var engine = new CastingEngine(config, NullLogger<CastingEngine>.Instance);

        engine.Cast("agent-1", "lead");

        Assert.Throws<NotSupportedException>(() => engine.Cast("agent-2", "tester"));
    }

    [Fact]
    public void Cast_WithAllowlistUniverse_UsesAllowedUniverse()
    {
        var config = new CastingConfig
        {
            AllowlistUniverses = ["StarWars", "Marvel"]
        };
        var engine = new CastingEngine(config, NullLogger<CastingEngine>.Instance);

        var member = engine.Cast("agent-1", "lead", "StarWars");

        Assert.Equal("StarWars", member.Universe);
    }

    [Fact]
    public void Cast_NoPreferredUniverse_NoAllowlist_ReturnsDefault()
    {
        var member = _engine.Cast("agent-1", "lead");
        Assert.Equal("default", member.Universe);
    }

    [Fact]
    public void CastMember_TraitsContainRoleAndUniverse()
    {
        var member = _engine.Cast("agent-1", "lead", "TestUniverse");

        Assert.Contains("role:lead", member.Traits);
        Assert.Contains("universe:TestUniverse", member.Traits);
    }

    [Fact]
    public void UpdateConfig_ChangesCapacity()
    {
        var engine = new CastingEngine(
            new CastingConfig { Capacity = 1, OverflowStrategy = OverflowStrategy.Reject },
            NullLogger<CastingEngine>.Instance);

        engine.Cast("agent-1", "lead");

        engine.UpdateConfig(new CastingConfig { Capacity = 5, OverflowStrategy = OverflowStrategy.Reject });

        var ex = Record.Exception(() => engine.Cast("agent-2", "tester"));
        Assert.Null(ex);
    }
}

#endregion

#region Platform — PlatformDetector & PlatformTypes

public sealed class PlatformDetectorTests
{
    [Fact]
    public void Detect_NonGitDirectory_ReturnsUnknown()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var result = PlatformDetector.Detect(tempDir);
            Assert.Equal(PlatformType.Unknown, result);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Detect_NullWorkingDir_DoesNotThrow()
    {
        var ex = Record.Exception(() => PlatformDetector.Detect(null));
        Assert.Null(ex);
    }

    [Fact]
    public void Detect_ReturnsValidEnumValue()
    {
        var result = PlatformDetector.Detect();
        Assert.True(Enum.IsDefined(result));
    }

    [Fact]
    public void PlatformType_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(PlatformType), PlatformType.GitHub));
        Assert.True(Enum.IsDefined(typeof(PlatformType), PlatformType.AzureDevOps));
        Assert.True(Enum.IsDefined(typeof(PlatformType), PlatformType.Local));
        Assert.True(Enum.IsDefined(typeof(PlatformType), PlatformType.Unknown));
    }

    [Fact]
    public void WorkItem_RecordCreation()
    {
        var item = new WorkItem
        {
            Id = "1",
            Title = "Test Item",
            State = "Open",
            Labels = ["bug", "p1"]
        };

        Assert.Equal("1", item.Id);
        Assert.Equal("Test Item", item.Title);
        Assert.Equal(2, item.Labels.Count);
    }

    [Fact]
    public void PullRequestInfo_RecordCreation()
    {
        var pr = new PullRequestInfo
        {
            Id = "42",
            Title = "Fix tests",
            SourceBranch = "feature",
            TargetBranch = "main",
            Author = "dev"
        };

        Assert.Equal("42", pr.Id);
        Assert.Equal("feature", pr.SourceBranch);
        Assert.Equal("main", pr.TargetBranch);
    }
}

#endregion

#region Remote — RemoteProtocol & RemoteBridge

public sealed class RemoteProtocolTests
{
    [Fact]
    public void RCMessage_RecordCreation()
    {
        var msg = new RCMessage
        {
            Type = "chat",
            Id = "msg-1",
            SessionId = "s1",
            AgentName = "agent-1",
            Content = "Hello"
        };

        Assert.Equal("chat", msg.Type);
        Assert.Equal("msg-1", msg.Id);
        Assert.Equal("Hello", msg.Content);
    }

    [Fact]
    public void RCMessage_DefaultTimestamp_IsRecent()
    {
        var msg = new RCMessage { Type = "ping", Id = "1" };
        Assert.True((DateTimeOffset.UtcNow - msg.Timestamp).TotalSeconds < 5);
    }

    [Fact]
    public void RCAgent_RecordCreation()
    {
        var agent = new RCAgent
        {
            Name = "bot-1",
            Role = "tester",
            Status = "active",
            SessionId = "s1"
        };

        Assert.Equal("bot-1", agent.Name);
        Assert.Equal("tester", agent.Role);
    }

    [Fact]
    public void RCServerEvent_RecordCreation()
    {
        var evt = new RCServerEvent
        {
            Event = RemoteEvents.Connected,
            SessionId = "s1"
        };

        Assert.Equal("connected", evt.Event);
    }

    [Fact]
    public void RCServerEvent_DataAsJsonElement_WorksCorrectly()
    {
        var jsonData = JsonDocument.Parse("{\"key\":\"value\",\"count\":42}").RootElement;
        var evt = new RCServerEvent
        {
            Event = RemoteEvents.Status,
            SessionId = "s1",
            Data = jsonData
        };

        Assert.NotNull(evt.Data);
        Assert.Equal(JsonValueKind.Object, evt.Data.Value.ValueKind);
        Assert.Equal("value", evt.Data.Value.GetProperty("key").GetString());
        Assert.Equal(42, evt.Data.Value.GetProperty("count").GetInt32());
    }

    [Fact]
    public void RCServerEvent_DataNull_WorksCorrectly()
    {
        var evt = new RCServerEvent
        {
            Event = RemoteEvents.Disconnected,
            SessionId = "s1",
            Data = null
        };

        Assert.Null(evt.Data);
    }

    [Fact]
    public void RCClientCommand_RecordCreation()
    {
        var cmd = new RCClientCommand
        {
            Command = RemoteCommands.Ping,
            TargetAgent = "agent-1",
            Parameters = new Dictionary<string, string> { ["key"] = "value" }
        };

        Assert.Equal("ping", cmd.Command);
        Assert.Equal("agent-1", cmd.TargetAgent);
    }

    [Fact]
    public void RemoteCommands_HasExpectedConstants()
    {
        Assert.Equal("ping", RemoteCommands.Ping);
        Assert.Equal("list-agents", RemoteCommands.ListAgents);
        Assert.Equal("send-message", RemoteCommands.SendMessage);
        Assert.Equal("get-status", RemoteCommands.GetStatus);
        Assert.Equal("shutdown", RemoteCommands.Shutdown);
    }

    [Fact]
    public void RemoteEvents_HasExpectedConstants()
    {
        Assert.Equal("connected", RemoteEvents.Connected);
        Assert.Equal("disconnected", RemoteEvents.Disconnected);
        Assert.Equal("message-received", RemoteEvents.MessageReceived);
        Assert.Equal("agent-spawned", RemoteEvents.AgentSpawned);
        Assert.Equal("agent-destroyed", RemoteEvents.AgentDestroyed);
        Assert.Equal("error", RemoteEvents.Error);
        Assert.Equal("pong", RemoteEvents.Pong);
        Assert.Equal("agents-listed", RemoteEvents.AgentsListed);
        Assert.Equal("status", RemoteEvents.Status);
    }
}

public sealed class RemoteBridgeTests
{
    private readonly Mock<ISquadClient> _mockClient = new();
    private readonly RemoteBridge _bridge;

    public RemoteBridgeTests()
    {
        _bridge = new RemoteBridge(_mockClient.Object, NullLogger<RemoteBridge>.Instance);
    }

    [Fact]
    public void Constructor_InitializesNotRunning()
    {
        Assert.False(_bridge.IsRunning);
    }

    [Fact]
    public void Start_SetsIsRunningTrue()
    {
        _bridge.Start();
        Assert.True(_bridge.IsRunning);
    }

    [Fact]
    public void Stop_SetsIsRunningFalse()
    {
        _bridge.Start();
        _bridge.Stop();
        Assert.False(_bridge.IsRunning);
    }

    [Fact]
    public async Task HandleCommandAsync_Ping_ReturnsPong()
    {
        var cmd = new RCClientCommand { Command = RemoteCommands.Ping };

        var result = await _bridge.HandleCommandAsync(cmd);

        Assert.Equal(RemoteEvents.Pong, result.Event);
    }

    [Fact]
    public async Task HandleCommandAsync_UnknownCommand_ReturnsError()
    {
        var cmd = new RCClientCommand { Command = "unknown-cmd" };

        var result = await _bridge.HandleCommandAsync(cmd);

        Assert.Equal(RemoteEvents.Error, result.Event);
    }

    [Fact]
    public async Task HandleCommandAsync_ListAgents_ReturnsAgentsList()
    {
        _mockClient.Setup(c => c.ListSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SquadSessionMetadata>
            {
                new() { SessionId = "s1", AgentName = "agent-1" },
                new() { SessionId = "s2", AgentName = "agent-2" }
            });

        var cmd = new RCClientCommand { Command = RemoteCommands.ListAgents };

        var result = await _bridge.HandleCommandAsync(cmd);

        Assert.Equal(RemoteEvents.AgentsListed, result.Event);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task HandleCommandAsync_GetStatus_ReturnsStatusEvent()
    {
        _mockClient.Setup(c => c.ListSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SquadSessionMetadata>());

        var cmd = new RCClientCommand { Command = RemoteCommands.GetStatus };

        var result = await _bridge.HandleCommandAsync(cmd);

        Assert.Equal(RemoteEvents.Status, result.Event);
    }
}

#endregion

#region Sharing — SquadExporter & SquadImporter

public sealed class SharingTests
{
    private static SquadConfig CreateTestConfig() => new()
    {
        Team = new TeamConfig { Name = "Test Squad", Description = "Test description" },
        Agents =
        [
            new AgentConfig { Name = "agent-1", Role = "lead", Prompt = "You are the lead." },
            new AgentConfig { Name = "agent-2", Role = "tester", Prompt = "You are a tester." }
        ]
    };

    [Fact]
    public void Export_ProducesExportedSquad()
    {
        var exporter = new SquadExporter(NullLogger<SquadExporter>.Instance);
        var config = CreateTestConfig();

        var result = exporter.Export(config, "test-author");

        Assert.Equal("Test Squad", result.Name);
        Assert.Equal("test-author", result.Author);
        Assert.Equal(2, result.Agents.Count);
        Assert.NotNull(result.ConfigJson);
        Assert.NotEmpty(result.ConfigJson);
    }

    [Fact]
    public void Export_AgentsMatchConfig()
    {
        var exporter = new SquadExporter(NullLogger<SquadExporter>.Instance);
        var config = CreateTestConfig();

        var result = exporter.Export(config);

        Assert.Equal("agent-1", result.Agents[0].Name);
        Assert.Equal("lead", result.Agents[0].Role);
        Assert.Equal("agent-2", result.Agents[1].Name);
    }

    [Fact]
    public void Export_ConfigJsonIsValidJson()
    {
        var exporter = new SquadExporter(NullLogger<SquadExporter>.Instance);
        var config = CreateTestConfig();

        var result = exporter.Export(config);

        var ex = Record.Exception(() => System.Text.Json.JsonDocument.Parse(result.ConfigJson));
        Assert.Null(ex);
    }

    [Fact]
    public void DeserializeConfig_RoundTrips()
    {
        var exporter = new SquadExporter(NullLogger<SquadExporter>.Instance);
        var importer = new SquadImporter(NullLogger<SquadImporter>.Instance);
        var config = CreateTestConfig();

        var exported = exporter.Export(config);
        var deserialized = importer.DeserializeConfig(exported);

        Assert.NotNull(deserialized);
        Assert.Equal("Test Squad", deserialized.Team.Name);
        Assert.Equal(2, deserialized.Agents.Count);
    }

    [Fact]
    public void ExportedSquad_RecordCreation()
    {
        var squad = new ExportedSquad
        {
            Name = "My Squad",
            Version = "1.0",
            Description = "A test squad",
            ConfigJson = "{}",
            Agents = [new ExportedAgent { Name = "a1", Role = "lead" }]
        };

        Assert.Equal("My Squad", squad.Name);
        Assert.Equal("1.0", squad.Version);
        Assert.Single(squad.Agents);
    }

    [Fact]
    public void ExportedSquad_DefaultTimestamp_IsRecent()
    {
        var squad = new ExportedSquad { Name = "X", Version = "1", ConfigJson = "{}" };
        Assert.True((DateTimeOffset.UtcNow - squad.ExportedAt).TotalSeconds < 5);
    }

    [Fact]
    public void ImportResult_SuccessCreation()
    {
        var result = new ImportResult
        {
            Success = true,
            Message = "OK",
            ImportedPath = "/path/to/file"
        };

        Assert.True(result.Success);
        Assert.Equal("OK", result.Message);
    }

    [Fact]
    public void ImportResult_FailureCreation()
    {
        var result = new ImportResult { Success = false, Message = "Not found" };
        Assert.False(result.Success);
    }

    [Fact]
    public void DeserializeConfig_InvalidJson_ReturnsNull()
    {
        var importer = new SquadImporter(NullLogger<SquadImporter>.Instance);
        var exported = new ExportedSquad
        {
            Name = "Bad",
            Version = "1",
            ConfigJson = "not-json"
        };

        var result = importer.DeserializeConfig(exported);

        Assert.Null(result);
    }
}

#endregion

#region Marketplace — ManifestValidator & MarketplaceTypes

public sealed class ManifestValidatorTests
{
    private static MarketplaceManifest CreateValidManifest() => new()
    {
        Name = "my-squad",
        Version = "1.0.0",
        Description = "A great squad",
        Author = "dev",
        Category = ManifestCategory.Development
    };

    [Fact]
    public void Validate_ValidManifest_ReturnsNoErrors()
    {
        var errors = ManifestValidator.Validate(CreateValidManifest());
        Assert.Empty(errors);
    }

    [Fact]
    public void IsValid_ValidManifest_ReturnsTrue()
    {
        Assert.True(ManifestValidator.IsValid(CreateValidManifest()));
    }

    [Fact]
    public void Validate_MissingName_ReturnsError()
    {
        var manifest = new MarketplaceManifest { Name = "", Version = "1.0.0" };

        var errors = ManifestValidator.Validate(manifest);

        Assert.Contains(errors, e => e.Contains("name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_MissingVersion_ReturnsError()
    {
        var manifest = new MarketplaceManifest { Name = "test", Version = "" };

        var errors = ManifestValidator.Validate(manifest);

        Assert.Contains(errors, e => e.Contains("version", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_NameTooLong_ReturnsError()
    {
        var manifest = new MarketplaceManifest
        {
            Name = new string('x', 129),
            Version = "1.0.0"
        };

        var errors = ManifestValidator.Validate(manifest);

        Assert.Contains(errors, e => e.Contains("128", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_DescriptionTooLong_ReturnsError()
    {
        var manifest = new MarketplaceManifest
        {
            Name = "test",
            Version = "1.0.0",
            Description = new string('x', 1025)
        };

        var errors = ManifestValidator.Validate(manifest);

        Assert.Contains(errors, e => e.Contains("1024", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_TooManyTags_ReturnsError()
    {
        var tags = Enumerable.Range(0, 21).Select(i => $"tag-{i}").ToList();
        var manifest = new MarketplaceManifest
        {
            Name = "test",
            Version = "1.0.0",
            Tags = tags
        };

        var errors = ManifestValidator.Validate(manifest);

        Assert.Contains(errors, e => e.Contains("20", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_EmptyTag_ReturnsError()
    {
        var manifest = new MarketplaceManifest
        {
            Name = "test",
            Version = "1.0.0",
            Tags = ["valid", ""]
        };

        var errors = ManifestValidator.Validate(manifest);

        Assert.Contains(errors, e => e.Contains("empty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_TagTooLong_ReturnsError()
    {
        var manifest = new MarketplaceManifest
        {
            Name = "test",
            Version = "1.0.0",
            Tags = [new string('t', 65)]
        };

        var errors = ManifestValidator.Validate(manifest);

        Assert.Contains(errors, e => e.Contains("64", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_EmptyCapabilityName_ReturnsError()
    {
        var manifest = new MarketplaceManifest
        {
            Name = "test",
            Version = "1.0.0",
            Capabilities = [new ManifestCapability { Name = "" }]
        };

        var errors = ManifestValidator.Validate(manifest);

        Assert.Contains(errors, e => e.Contains("capability", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(ManifestCategory.General)]
    [InlineData(ManifestCategory.Development)]
    [InlineData(ManifestCategory.Testing)]
    [InlineData(ManifestCategory.Documentation)]
    [InlineData(ManifestCategory.Security)]
    [InlineData(ManifestCategory.DevOps)]
    [InlineData(ManifestCategory.Data)]
    [InlineData(ManifestCategory.AI)]
    public void ManifestCategory_AllValues_AreDefined(ManifestCategory category)
    {
        Assert.True(Enum.IsDefined(category));
    }

    [Fact]
    public void ManifestCapability_RecordCreation()
    {
        var cap = new ManifestCapability
        {
            Name = "code-review",
            Description = "Reviews code",
            Required = true
        };

        Assert.Equal("code-review", cap.Name);
        Assert.True(cap.Required);
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAll()
    {
        var manifest = new MarketplaceManifest { Name = "", Version = "" };

        var errors = ManifestValidator.Validate(manifest);

        Assert.True(errors.Count >= 2);
    }
}

#endregion
