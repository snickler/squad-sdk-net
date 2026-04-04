using Squad.SDK.NET.Builder;
using Squad.SDK.NET.Config;
using Squad.SDK.NET.Hooks;
using Squad.SDK.NET.Utils;

namespace Squad.SDK.NET.Tests;

#region Config Record Defaults

public sealed class BudgetConfigTests
{
    [Fact]
    public void Ctor_Default_AllPropertiesAreNull()
    {
        // Act
        var config = new BudgetConfig();

        // Assert
        Assert.Null(config.PerAgentSpawn);
        Assert.Null(config.PerSession);
        Assert.Null(config.WarnAt);
    }

    [Fact]
    public void With_OverrideValues_ReturnsNewRecordWithValues()
    {
        // Arrange
        var original = new BudgetConfig();

        // Act
        var updated = original with { PerAgentSpawn = 10m, PerSession = 100m, WarnAt = 80m };

        // Assert
        Assert.Equal(10m, updated.PerAgentSpawn);
        Assert.Equal(100m, updated.PerSession);
        Assert.Equal(80m, updated.WarnAt);
        Assert.Null(original.PerAgentSpawn);
    }
}

public sealed class CeremonyConfigTests
{
    [Fact]
    public void Ctor_Default_ListsAreEmpty()
    {
        // Act
        var config = new CeremonyConfig { Name = "standup" };

        // Assert
        Assert.Equal("standup", config.Name);
        Assert.Null(config.Trigger);
        Assert.Null(config.Schedule);
        Assert.Empty(config.Participants);
        Assert.Empty(config.Agenda);
        Assert.Null(config.Hooks);
    }

    [Fact]
    public void With_SetHooks_ReturnsCopyWithHooks()
    {
        // Arrange
        var hooks = new PolicyConfig { ScrubPii = true };
        var config = new CeremonyConfig { Name = "retro" };

        // Act
        var updated = config with { Hooks = hooks, Schedule = "weekly" };

        // Assert
        Assert.NotNull(updated.Hooks);
        Assert.True(updated.Hooks.ScrubPii);
        Assert.Equal("weekly", updated.Schedule);
    }
}

public sealed class CastingConfigTests
{
    [Fact]
    public void Ctor_Default_OverflowStrategyIsRotate()
    {
        // Act
        var config = new CastingConfig();

        // Assert
        Assert.Equal(OverflowStrategy.Rotate, config.OverflowStrategy);
        Assert.Empty(config.AllowlistUniverses);
        Assert.Null(config.Capacity);
    }

    [Theory]
    [InlineData(OverflowStrategy.Rotate)]
    [InlineData(OverflowStrategy.Queue)]
    [InlineData(OverflowStrategy.Reject)]
    public void With_SetOverflowStrategy_ReflectsValue(OverflowStrategy strategy)
    {
        // Act
        var config = new CastingConfig { OverflowStrategy = strategy };

        // Assert
        Assert.Equal(strategy, config.OverflowStrategy);
    }
}

public sealed class TelemetryConfigTests
{
    [Fact]
    public void Ctor_Default_EnabledTrue_SampleRateOne()
    {
        // Act
        var config = new TelemetryConfig();

        // Assert
        Assert.True(config.Enabled);
        Assert.Null(config.Endpoint);
        Assert.Null(config.ServiceName);
        Assert.Equal(1.0, config.SampleRate);
        Assert.False(config.AspireDefaults);
    }

    [Fact]
    public void With_DisableAndSetEndpoint_ReflectsValues()
    {
        // Arrange
        var config = new TelemetryConfig();

        // Act
        var updated = config with { Enabled = false, Endpoint = "https://otel.local" };

        // Assert
        Assert.False(updated.Enabled);
        Assert.Equal("https://otel.local", updated.Endpoint);
    }
}

public sealed class DefaultsConfigTests
{
    [Fact]
    public void Ctor_Default_AllPropertiesNull()
    {
        // Act
        var config = new DefaultsConfig();

        // Assert
        Assert.Null(config.Model);
        Assert.Null(config.ModelPreference);
        Assert.Null(config.Budget);
    }

    [Fact]
    public void With_SetNestedBudget_ReflectsValues()
    {
        // Act
        var config = new DefaultsConfig
        {
            Model = "gpt-5",
            Budget = new BudgetConfig { PerSession = 50m }
        };

        // Assert
        Assert.Equal("gpt-5", config.Model);
        Assert.Equal(50m, config.Budget!.PerSession);
    }
}

public sealed class ModelPreferenceTests
{
    [Fact]
    public void Ctor_RequiredPreferred_OptionalFieldsNull()
    {
        // Act
        var pref = new ModelPreference { Preferred = "gpt-5" };

        // Assert
        Assert.Equal("gpt-5", pref.Preferred);
        Assert.Null(pref.Rationale);
        Assert.Null(pref.Fallback);
    }

    [Fact]
    public void With_SetFallbackAndRationale_ReflectsValues()
    {
        // Arrange
        var pref = new ModelPreference { Preferred = "gpt-5" };

        // Act
        var updated = pref with { Rationale = "best quality", Fallback = "gpt-4.1" };

        // Assert
        Assert.Equal("gpt-5", updated.Preferred);
        Assert.Equal("best quality", updated.Rationale);
        Assert.Equal("gpt-4.1", updated.Fallback);
    }
}

public sealed class SkillConfigTests
{
    [Fact]
    public void Ctor_Default_ConfidenceIsMedium_ToolsEmpty()
    {
        // Act
        var config = new SkillConfig { Name = "coding" };

        // Assert
        Assert.Equal("coding", config.Name);
        Assert.Equal(SkillConfidenceLevel.Medium, config.Confidence);
        Assert.Empty(config.Tools);
        Assert.Null(config.Description);
        Assert.Null(config.Domain);
        Assert.Null(config.Source);
        Assert.Null(config.Content);
    }

    [Theory]
    [InlineData(SkillConfidenceLevel.Low)]
    [InlineData(SkillConfidenceLevel.Medium)]
    [InlineData(SkillConfidenceLevel.High)]
    public void Confidence_SetValue_ReflectsCorrectly(SkillConfidenceLevel level)
    {
        // Act
        var config = new SkillConfig { Name = "test", Confidence = level };

        // Assert
        Assert.Equal(level, config.Confidence);
    }
}

public sealed class AgentCapabilityTests
{
    [Fact]
    public void Ctor_Default_EnabledIsTrue()
    {
        // Act
        var cap = new AgentCapability { Name = "code-review" };

        // Assert
        Assert.Equal("code-review", cap.Name);
        Assert.True(cap.Enabled);
        Assert.Null(cap.Description);
    }

    [Fact]
    public void With_DisableCapability_ReflectsValue()
    {
        // Arrange
        var cap = new AgentCapability { Name = "deploy", Enabled = true };

        // Act
        var updated = cap with { Enabled = false, Description = "disabled for now" };

        // Assert
        Assert.False(updated.Enabled);
        Assert.Equal("disabled for now", updated.Description);
    }
}

public sealed class AgentStatusEnumTests
{
    [Theory]
    [InlineData(AgentStatus.Active, 0)]
    [InlineData(AgentStatus.Inactive, 1)]
    [InlineData(AgentStatus.Retired, 2)]
    public void EnumValues_HaveExpectedOrdinals(AgentStatus status, int expected)
    {
        Assert.Equal(expected, (int)status);
    }

    [Fact]
    public void Enum_HasExactlyThreeMembers()
    {
        var values = Enum.GetValues<AgentStatus>();
        Assert.Equal(3, values.Length);
    }
}

public sealed class OverflowStrategyEnumTests
{
    [Theory]
    [InlineData(OverflowStrategy.Rotate, 0)]
    [InlineData(OverflowStrategy.Queue, 1)]
    [InlineData(OverflowStrategy.Reject, 2)]
    public void EnumValues_HaveExpectedOrdinals(OverflowStrategy strategy, int expected)
    {
        Assert.Equal(expected, (int)strategy);
    }

    [Fact]
    public void Enum_HasExactlyThreeMembers()
    {
        var values = Enum.GetValues<OverflowStrategy>();
        Assert.Equal(3, values.Length);
    }
}

public sealed class SkillConfidenceLevelEnumTests
{
    [Theory]
    [InlineData(SkillConfidenceLevel.Low, 0)]
    [InlineData(SkillConfidenceLevel.Medium, 1)]
    [InlineData(SkillConfidenceLevel.High, 2)]
    public void EnumValues_HaveExpectedOrdinals(SkillConfidenceLevel level, int expected)
    {
        Assert.Equal(expected, (int)level);
    }
}

public sealed class HooksDefinitionTests
{
    [Fact]
    public void Ctor_Default_BooleansFalse_ListsNull()
    {
        // Act
        var hooks = new HooksDefinition();

        // Assert
        Assert.Null(hooks.AllowedWritePaths);
        Assert.Null(hooks.BlockedCommands);
        Assert.Null(hooks.MaxAskUserPerSession);
        Assert.False(hooks.ScrubPii);
        Assert.False(hooks.ReviewerLockout);
    }

    [Fact]
    public void ToPolicyConfig_MapsAllProperties()
    {
        // Arrange
        var hooks = new HooksDefinition
        {
            AllowedWritePaths = ["src/", "tests/"],
            BlockedCommands = ["rm -rf"],
            MaxAskUserPerSession = 3,
            ScrubPii = true,
            ReviewerLockout = true
        };

        // Act
        var policy = hooks.ToPolicyConfig();

        // Assert
        Assert.Equal(hooks.AllowedWritePaths, policy.AllowedWritePaths);
        Assert.Equal(hooks.BlockedCommands, policy.BlockedCommands);
        Assert.Equal(3, policy.MaxAskUserPerSession);
        Assert.True(policy.ScrubPii);
        Assert.True(policy.ReviewerLockout);
    }

    [Fact]
    public void ToPolicyConfig_EmptyHooks_ProducesEmptyPolicy()
    {
        // Arrange
        var hooks = new HooksDefinition();

        // Act
        var policy = hooks.ToPolicyConfig();

        // Assert
        Assert.Null(policy.AllowedWritePaths);
        Assert.Null(policy.BlockedCommands);
        Assert.Null(policy.MaxAskUserPerSession);
        Assert.False(policy.ScrubPii);
        Assert.False(policy.ReviewerLockout);
    }
}

#endregion

#region Builder Tests

public sealed class BudgetBuilderTests
{
    [Fact]
    public void FluentChain_SetsAllValues()
    {
        // Arrange & Act
        var builder = new BudgetBuilder()
            .PerAgentSpawn(5m)
            .PerSession(50m)
            .WarnAt(40m);

        // Build is internal — test via SquadBuilder or DefaultsBuilder integration
        // Verify fluent return type
        Assert.IsType<BudgetBuilder>(builder);
    }
}

public sealed class CeremonyBuilderTests
{
    [Fact]
    public void FluentChain_ReturnsBuilderType()
    {
        // Act
        var builder = new CeremonyBuilder()
            .Name("standup")
            .Trigger("daily")
            .Schedule("09:00")
            .Participants("alice", "bob")
            .Agenda("updates", "blockers");

        // Assert
        Assert.IsType<CeremonyBuilder>(builder);
    }
}

public sealed class CastingBuilderTests
{
    [Fact]
    public void FluentChain_ReturnsBuilderType()
    {
        // Act
        var builder = new CastingBuilder()
            .AllowlistUniverses("github", "gitlab")
            .OverflowStrategy(OverflowStrategy.Queue)
            .Capacity(10);

        // Assert
        Assert.IsType<CastingBuilder>(builder);
    }
}

public sealed class TelemetryBuilderTests
{
    [Fact]
    public void FluentChain_ReturnsBuilderType()
    {
        // Act
        var builder = new TelemetryBuilder()
            .Enabled(true)
            .Endpoint("https://otel.local")
            .ServiceName("squad-sdk")
            .SampleRate(0.5)
            .AspireDefaults();

        // Assert
        Assert.IsType<TelemetryBuilder>(builder);
    }
}

public sealed class SkillBuilderTests
{
    [Fact]
    public void FluentChain_ReturnsBuilderType()
    {
        // Act
        var builder = new SkillBuilder()
            .Name("coding")
            .Description("writes code")
            .Domain("backend")
            .Confidence(SkillConfidenceLevel.High)
            .Source("inline")
            .Content("some content")
            .Tools("grep", "view");

        // Assert
        Assert.IsType<SkillBuilder>(builder);
    }
}

public sealed class DefaultsBuilderTests
{
    [Fact]
    public void FluentChain_ModelString_ReturnsBuilderType()
    {
        // Act
        var builder = new DefaultsBuilder().Model("gpt-5");

        // Assert
        Assert.IsType<DefaultsBuilder>(builder);
    }

    [Fact]
    public void FluentChain_ModelPreference_ReturnsBuilderType()
    {
        // Arrange
        var pref = new ModelPreference { Preferred = "gpt-5", Fallback = "gpt-4.1" };

        // Act
        var builder = new DefaultsBuilder().Model(pref);

        // Assert
        Assert.IsType<DefaultsBuilder>(builder);
    }

    [Fact]
    public void FluentChain_BudgetAction_ReturnsBuilderType()
    {
        // Act
        var builder = new DefaultsBuilder()
            .Budget(b => b.PerSession(100m));

        // Assert
        Assert.IsType<DefaultsBuilder>(builder);
    }

    [Fact]
    public void FluentChain_BudgetConfig_ReturnsBuilderType()
    {
        // Act
        var builder = new DefaultsBuilder()
            .Budget(new BudgetConfig { PerSession = 100m });

        // Assert
        Assert.IsType<DefaultsBuilder>(builder);
    }
}

public sealed class HooksBuilderTests
{
    [Fact]
    public void FluentChain_ReturnsBuilderType()
    {
        // Act
        var builder = new HooksBuilder()
            .AllowedWritePaths("src/", "tests/")
            .BlockedCommands("rm -rf")
            .MaxAskUser(5)
            .ScrubPii()
            .ReviewerLockout();

        // Assert
        Assert.IsType<HooksBuilder>(builder);
    }
}

public sealed class BuilderValidationErrorTests
{
    [Fact]
    public void Ctor_MultipleErrors_SetsPropertiesAndMessage()
    {
        // Arrange
        var errors = new List<string> { "Name is required", "Role is required" }.AsReadOnly();

        // Act
        var ex = new BuilderValidationError("AgentBuilder", errors);

        // Assert
        Assert.Equal("AgentBuilder", ex.BuilderName);
        Assert.Equal(2, ex.Errors.Count);
        Assert.Contains("Name is required", ex.Errors);
        Assert.Contains("AgentBuilder validation failed", ex.Message);
    }

    [Fact]
    public void Ctor_SingleError_WrapsInList()
    {
        // Act
        var ex = new BuilderValidationError("SkillBuilder", "Name is required");

        // Assert
        Assert.Equal("SkillBuilder", ex.BuilderName);
        Assert.Single(ex.Errors);
        Assert.Equal("Name is required", ex.Errors[0]);
    }

    [Fact]
    public void IsInvalidOperationException_True()
    {
        // Act
        var ex = new BuilderValidationError("Test", "error");

        // Assert
        Assert.IsAssignableFrom<InvalidOperationException>(ex);
    }

    [Fact]
    public void Message_ContainsBuilderNameAndErrors()
    {
        // Act
        var ex = new BuilderValidationError("CeremonyBuilder", new[] { "a", "b" });

        // Assert
        Assert.Contains("CeremonyBuilder", ex.Message);
        Assert.Contains("a", ex.Message);
        Assert.Contains("b", ex.Message);
    }
}

#endregion

#region AgentBuilder Integration Tests

public sealed class AgentBuilderIntegrationTests
{
    [Fact]
    public void WithCapabilities_AddsCapabilities()
    {
        // Arrange & Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithAgent(a => a
                .Name("agent1")
                .Role("dev")
                .Capabilities(
                    new AgentCapability { Name = "code-review" },
                    new AgentCapability { Name = "deploy", Enabled = false }))
            .Build();

        // Assert
        var agent = config.Agents[0];
        Assert.NotNull(agent.Capabilities);
        Assert.Equal(2, agent.Capabilities.Count);
        Assert.Equal("code-review", agent.Capabilities[0].Name);
        Assert.False(agent.Capabilities[1].Enabled);
    }

    [Fact]
    public void WithBudgetAction_SetsBudget()
    {
        // Arrange & Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithAgent(a => a
                .Name("agent1")
                .Role("dev")
                .Budget(b => b.PerAgentSpawn(2m).WarnAt(1.5m)))
            .Build();

        // Assert
        var agent = config.Agents[0];
        Assert.NotNull(agent.Budget);
        Assert.Equal(2m, agent.Budget.PerAgentSpawn);
        Assert.Equal(1.5m, agent.Budget.WarnAt);
    }

    [Fact]
    public void WithBudgetConfig_SetsBudget()
    {
        // Arrange
        var budget = new BudgetConfig { PerSession = 99m };

        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithAgent(a => a.Name("agent1").Role("dev").Budget(budget))
            .Build();

        // Assert
        Assert.Equal(99m, config.Agents[0].Budget!.PerSession);
    }

    [Fact]
    public void WithStatus_SetsAgentStatus()
    {
        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithAgent(a => a.Name("agent1").Role("dev").Status(AgentStatus.Retired))
            .Build();

        // Assert
        Assert.Equal(AgentStatus.Retired, config.Agents[0].Status);
    }

    [Fact]
    public void WithStatus_Default_IsActive()
    {
        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithAgent(a => a.Name("agent1").Role("dev"))
            .Build();

        // Assert
        Assert.Equal(AgentStatus.Active, config.Agents[0].Status);
    }

    [Fact]
    public void WithCharter_SetsCharter()
    {
        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithAgent(a => a.Name("agent1").Role("dev").Charter("Ship quality code"))
            .Build();

        // Assert
        Assert.Equal("Ship quality code", config.Agents[0].Charter);
    }

    [Fact]
    public void Build_MissingName_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            SquadBuilder.Create()
                .WithTeam(t => t.Name("T"))
                .WithAgent(a => a.Role("dev"))
                .Build());
    }

    [Fact]
    public void Build_MissingRole_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            SquadBuilder.Create()
                .WithTeam(t => t.Name("T"))
                .WithAgent(a => a.Name("agent1"))
                .Build());
    }
}

#endregion

#region SquadBuilder New Integration Tests

public sealed class SquadBuilderNewFeaturesTests
{
    [Fact]
    public void WithDefaults_SetsDefaultsConfig()
    {
        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithDefaults(d => d.Model("gpt-5"))
            .Build();

        // Assert
        Assert.NotNull(config.Defaults);
        Assert.Equal("gpt-5", config.Defaults.Model);
    }

    [Fact]
    public void WithDefaults_ModelPreference_SetsModelAndPreference()
    {
        // Arrange
        var pref = new ModelPreference { Preferred = "gpt-5", Fallback = "gpt-4.1", Rationale = "quality" };

        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithDefaults(d => d.Model(pref))
            .Build();

        // Assert
        Assert.NotNull(config.Defaults);
        Assert.Equal("gpt-5", config.Defaults.Model);
        Assert.NotNull(config.Defaults.ModelPreference);
        Assert.Equal("gpt-4.1", config.Defaults.ModelPreference.Fallback);
    }

    [Fact]
    public void WithDefaults_BudgetAction_SetsNestedBudget()
    {
        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithDefaults(d => d.Budget(b => b.PerSession(200m).WarnAt(150m)))
            .Build();

        // Assert
        Assert.NotNull(config.Defaults?.Budget);
        Assert.Equal(200m, config.Defaults.Budget.PerSession);
        Assert.Equal(150m, config.Defaults.Budget.WarnAt);
    }

    [Fact]
    public void WithCeremony_AddsSingleCeremony()
    {
        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithCeremony(c => c.Name("standup").Trigger("daily").Schedule("09:00").Participants("alice", "bob"))
            .Build();

        // Assert
        Assert.NotNull(config.Ceremonies);
        Assert.Single(config.Ceremonies);
        Assert.Equal("standup", config.Ceremonies[0].Name);
        Assert.Equal("daily", config.Ceremonies[0].Trigger);
        Assert.Equal("09:00", config.Ceremonies[0].Schedule);
        Assert.Equal(2, config.Ceremonies[0].Participants.Count);
    }

    [Fact]
    public void WithCeremony_MultipleCalls_AddsMultipleCeremonies()
    {
        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithCeremony(c => c.Name("standup").Trigger("daily"))
            .WithCeremony(c => c.Name("retro").Schedule("bi-weekly"))
            .Build();

        // Assert
        Assert.NotNull(config.Ceremonies);
        Assert.Equal(2, config.Ceremonies.Count);
    }

    [Fact]
    public void WithCeremony_MissingName_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            SquadBuilder.Create()
                .WithTeam(t => t.Name("T"))
                .WithCeremony(c => c.Trigger("daily"))
                .Build());
    }

    [Fact]
    public void WithCeremony_AgendaItems_ArePreserved()
    {
        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithCeremony(c => c.Name("retro").Agenda("what went well", "what to improve", "action items"))
            .Build();

        // Assert
        Assert.Equal(3, config.Ceremonies![0].Agenda.Count);
        Assert.Contains("action items", config.Ceremonies[0].Agenda);
    }

    [Fact]
    public void WithCasting_SetsCastingConfig()
    {
        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithCasting(c => c
                .AllowlistUniverses("github", "gitlab")
                .OverflowStrategy(OverflowStrategy.Reject)
                .Capacity(5))
            .Build();

        // Assert
        Assert.NotNull(config.Casting);
        Assert.Equal(2, config.Casting.AllowlistUniverses.Count);
        Assert.Equal(OverflowStrategy.Reject, config.Casting.OverflowStrategy);
        Assert.Equal(5, config.Casting.Capacity);
    }

    [Fact]
    public void WithTelemetry_SetsTelemetryConfig()
    {
        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithTelemetry(t => t
                .Enabled(true)
                .Endpoint("https://otel.local")
                .ServiceName("squad-sdk")
                .SampleRate(0.5)
                .AspireDefaults())
            .Build();

        // Assert
        Assert.NotNull(config.Telemetry);
        Assert.True(config.Telemetry.Enabled);
        Assert.Equal("https://otel.local", config.Telemetry.Endpoint);
        Assert.Equal("squad-sdk", config.Telemetry.ServiceName);
        Assert.Equal(0.5, config.Telemetry.SampleRate);
        Assert.True(config.Telemetry.AspireDefaults);
    }

    [Fact]
    public void WithSkill_AddsSingleSkill()
    {
        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithSkill(s => s
                .Name("coding")
                .Description("writes code")
                .Domain("backend")
                .Confidence(SkillConfidenceLevel.High)
                .Tools("grep", "view"))
            .Build();

        // Assert
        Assert.NotNull(config.Skills);
        Assert.Single(config.Skills);
        Assert.Equal("coding", config.Skills[0].Name);
        Assert.Equal(SkillConfidenceLevel.High, config.Skills[0].Confidence);
        Assert.Equal(2, config.Skills[0].Tools.Count);
    }

    [Fact]
    public void WithSkill_MissingName_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            SquadBuilder.Create()
                .WithTeam(t => t.Name("T"))
                .WithSkill(s => s.Description("no name"))
                .Build());
    }

    [Fact]
    public void WithSkill_MultipleCalls_AddsMultipleSkills()
    {
        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithSkill(s => s.Name("coding").Domain("backend"))
            .WithSkill(s => s.Name("review").Domain("quality"))
            .Build();

        // Assert
        Assert.Equal(2, config.Skills!.Count);
    }

    [Fact]
    public void WithBudget_SetsTopLevelBudget()
    {
        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithBudget(b => b.PerSession(500m).WarnAt(400m).PerAgentSpawn(10m))
            .Build();

        // Assert
        Assert.NotNull(config.Budget);
        Assert.Equal(500m, config.Budget.PerSession);
        Assert.Equal(400m, config.Budget.WarnAt);
        Assert.Equal(10m, config.Budget.PerAgentSpawn);
    }

    [Fact]
    public void WithHooksAction_SetsHooksDefinition()
    {
        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithHooks(h => h
                .AllowedWritePaths("src/")
                .BlockedCommands("rm -rf")
                .MaxAskUser(3)
                .ScrubPii()
                .ReviewerLockout())
            .Build();

        // Assert
        Assert.NotNull(config.Hooks);
        Assert.NotNull(config.Hooks.AllowedWritePaths);
        Assert.Single(config.Hooks.AllowedWritePaths);
        Assert.NotNull(config.Hooks.BlockedCommands);
        Assert.Equal(3, config.Hooks.MaxAskUserPerSession);
        Assert.True(config.Hooks.ScrubPii);
        Assert.True(config.Hooks.ReviewerLockout);
    }

    [Fact]
    public void WithHooksAction_NoPaths_AllowedWritePathsNull()
    {
        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .WithHooks(h => h.ScrubPii())
            .Build();

        // Assert
        Assert.NotNull(config.Hooks);
        Assert.Null(config.Hooks.AllowedWritePaths);
        Assert.Null(config.Hooks.BlockedCommands);
    }

    [Fact]
    public void NoCeremonies_CeremoniesIsNull()
    {
        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .Build();

        // Assert
        Assert.Null(config.Ceremonies);
    }

    [Fact]
    public void NoSkills_SkillsIsNull()
    {
        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("T"))
            .Build();

        // Assert
        Assert.Null(config.Skills);
    }

    [Fact]
    public void FullFluentChain_AllNewFeatures_ProducesValidConfig()
    {
        // Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("SuperTeam").Description("end-to-end"))
            .WithAgent(a => a
                .Name("coder")
                .Role("backend")
                .Charter("Write clean code")
                .Status(AgentStatus.Active)
                .Capabilities(new AgentCapability { Name = "coding" })
                .Budget(b => b.PerAgentSpawn(1m)))
            .WithDefaults(d => d
                .Model(new ModelPreference { Preferred = "gpt-5", Fallback = "gpt-4.1" })
                .Budget(b => b.PerSession(100m)))
            .WithCeremony(c => c.Name("standup").Trigger("daily").Participants("coder"))
            .WithCasting(c => c.AllowlistUniverses("github").OverflowStrategy(OverflowStrategy.Queue).Capacity(3))
            .WithTelemetry(t => t.ServiceName("squad-sdk").SampleRate(0.8))
            .WithSkill(s => s.Name("dotnet").Confidence(SkillConfidenceLevel.High).Tools("dotnet"))
            .WithBudget(b => b.PerSession(500m))
            .WithHooks(h => h.ScrubPii().MaxAskUser(5))
            .Build();

        // Assert
        Assert.Equal("SuperTeam", config.Team.Name);
        Assert.Single(config.Agents);
        Assert.NotNull(config.Defaults);
        Assert.NotNull(config.Ceremonies);
        Assert.NotNull(config.Casting);
        Assert.NotNull(config.Telemetry);
        Assert.NotNull(config.Skills);
        Assert.NotNull(config.Budget);
        Assert.NotNull(config.Hooks);
    }
}

#endregion

#region StringUtils Tests

public sealed class StringUtilsTests
{
    [Fact]
    public void NormalizeEol_CrLf_ReplacedWithLf()
    {
        // Arrange
        var input = "line1\r\nline2\r\nline3";

        // Act
        var result = StringUtils.NormalizeEol(input);

        // Assert
        Assert.Equal("line1\nline2\nline3", result);
    }

    [Fact]
    public void NormalizeEol_CrOnly_ReplacedWithLf()
    {
        // Arrange
        var input = "line1\rline2\rline3";

        // Act
        var result = StringUtils.NormalizeEol(input);

        // Assert
        Assert.Equal("line1\nline2\nline3", result);
    }

    [Fact]
    public void NormalizeEol_LfOnly_Unchanged()
    {
        // Arrange
        var input = "line1\nline2";

        // Act
        var result = StringUtils.NormalizeEol(input);

        // Assert
        Assert.Equal("line1\nline2", result);
    }

    [Fact]
    public void NormalizeEol_MixedEol_AllNormalized()
    {
        // Arrange
        var input = "a\r\nb\rc\nd";

        // Act
        var result = StringUtils.NormalizeEol(input);

        // Assert
        Assert.Equal("a\nb\nc\nd", result);
    }

    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("Hello  World", "hello-world")]
    [InlineData("Hello---World", "hello-world")]
    [InlineData("  Hello World  ", "hello-world")]
    [InlineData("CamelCase", "camelcase")]
    [InlineData("with.dots.and_underscores", "with-dots-and-underscores")]
    [InlineData("special!@#chars", "special-chars")]
    public void Slugify_VariousInputs_ProducesExpectedSlugs(string input, string expected)
    {
        // Act
        var result = StringUtils.Slugify(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Slugify_EmptyOrWhitespace_ReturnsEmpty(string? input)
    {
        // Act
        var result = StringUtils.Slugify(input!);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SafeTimestamp_SpecificDate_ReturnsExpectedFormat()
    {
        // Arrange
        var ts = new DateTimeOffset(2025, 7, 15, 14, 30, 45, TimeSpan.Zero);

        // Act
        var result = StringUtils.SafeTimestamp(ts);

        // Assert
        Assert.Equal("2025-07-15-143045", result);
    }

    [Fact]
    public void SafeTimestamp_NoArgument_ReturnsCurrentTimestamp()
    {
        // Act
        var result = StringUtils.SafeTimestamp();

        // Assert
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}-\d{6}$", result);
    }

    [Fact]
    public void SafeTimestamp_NullArgument_ReturnsCurrentTimestamp()
    {
        // Act
        var result = StringUtils.SafeTimestamp(null);

        // Assert
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}-\d{6}$", result);
    }
}

#endregion
