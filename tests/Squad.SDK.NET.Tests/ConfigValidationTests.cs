using Squad.SDK.NET.Config;
using Squad.SDK.NET.Coordinator;
using Xunit;

namespace Squad.SDK.NET.Tests;

public sealed class ConfigValidationTests
{
    [Fact]
    public void SquadConfig_DefaultVersion_IsOnePointZero()
    {
        // Arrange & Act
        var config = new SquadConfig
        {
            Team = new TeamConfig { Name = "Test" }
        };

        // Assert
        Assert.Equal("1.0", config.Version);
    }

    [Fact]
    public void TeamConfig_DefaultDefaultTier_IsStandard()
    {
        // Arrange & Act
        var config = new TeamConfig { Name = "Test" };

        // Assert
        Assert.Equal(ModelTier.Standard, config.DefaultTier);
    }

    [Fact]
    public void RoutingConfig_DefaultFallbackBehavior_IsCoordinator()
    {
        // Arrange & Act
        var config = new RoutingConfig();

        // Assert
        Assert.Equal(RoutingFallbackBehavior.Coordinator, config.FallbackBehavior);
    }

    [Fact]
    public void ModelSelectionConfig_DefaultDefaultModel_IsGptFive()
    {
        // Arrange & Act
        var config = new ModelSelectionConfig();

        // Assert
        Assert.Equal("gpt-5", config.DefaultModel);
    }

    [Fact]
    public void Validate_ValidConfig_ReturnsEmptyList()
    {
        // Arrange
        var config = new SquadConfig
        {
            Team = new TeamConfig { Name = "ValidTeam" },
            Agents =
            [
                new AgentConfig { Name = "agent1", Role = "Backend", Prompt = "work" }
            ]
        };

        // Act
        var errors = ConfigLoader.Validate(config);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MissingTeamName_ReturnsError()
    {
        // Arrange
        var config = new SquadConfig
        {
            Team = new TeamConfig { Name = "" }
        };

        // Act
        var errors = ConfigLoader.Validate(config);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Team.Name"));
    }

    [Fact]
    public void Validate_AgentMissingName_ReturnsError()
    {
        // Arrange
        var config = new SquadConfig
        {
            Team = new TeamConfig { Name = "Test" },
            Agents =
            [
                new AgentConfig { Name = "", Role = "Backend", Prompt = "work" }
            ]
        };

        // Act
        var errors = ConfigLoader.Validate(config);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Name"));
    }

    [Fact]
    public void Validate_AgentMissingRole_ReturnsError()
    {
        // Arrange
        var config = new SquadConfig
        {
            Team = new TeamConfig { Name = "Test" },
            Agents =
            [
                new AgentConfig { Name = "agent1", Role = "", Prompt = "work" }
            ]
        };

        // Act
        var errors = ConfigLoader.Validate(config);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Role"));
    }

    [Fact]
    public void Validate_RoutingRuleReferencesUnknownAgent_ReturnsError()
    {
        // Arrange
        var config = new SquadConfig
        {
            Team = new TeamConfig { Name = "Test" },
            Agents =
            [
                new AgentConfig { Name = "agent1", Role = "Backend", Prompt = "work" }
            ],
            Routing = new RoutingConfig
            {
                Rules =
                [
                    new RoutingRule
                    {
                        WorkType = "backend",
                        Agents = ["nonexistent-agent"]
                    }
                ]
            }
        };

        // Act
        var errors = ConfigLoader.Validate(config);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("unknown agent"));
    }

    [Fact]
    public void Validate_DefaultAgentUnknown_ReturnsError()
    {
        // Arrange
        var config = new SquadConfig
        {
            Team = new TeamConfig { Name = "Test" },
            Agents =
            [
                new AgentConfig { Name = "agent1", Role = "Backend", Prompt = "work" }
            ],
            Routing = new RoutingConfig
            {
                DefaultAgent = "unknown-agent"
            }
        };

        // Act
        var errors = ConfigLoader.Validate(config);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("DefaultAgent"));
    }

    [Fact]
    public void RoutingRule_Construction_SetsProperties()
    {
        // Arrange & Act
        var rule = new RoutingRule
        {
            WorkType = "backend-api",
            Agents = ["agent1", "agent2"],
            Tier = ResponseTier.Full,
            Priority = 10
        };

        // Assert
        Assert.Equal("backend-api", rule.WorkType);
        Assert.Equal(2, rule.Agents.Count);
        Assert.Equal(ResponseTier.Full, rule.Tier);
        Assert.Equal(10, rule.Priority);
    }

    [Fact]
    public void RoutingDecision_Construction_SetsProperties()
    {
        // Arrange & Act
        var decision = new RoutingDecision
        {
            Tier = ResponseTier.Standard,
            Agents = ["agent1", "agent2"],
            Parallel = true,
            Rationale = "Test rationale"
        };

        // Assert
        Assert.Equal(ResponseTier.Standard, decision.Tier);
        Assert.Equal(2, decision.Agents.Count);
        Assert.True(decision.Parallel);
        Assert.Equal("Test rationale", decision.Rationale);
    }

    [Fact]
    public void ResponseTier_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)ResponseTier.Direct);
        Assert.Equal(1, (int)ResponseTier.Lightweight);
        Assert.Equal(2, (int)ResponseTier.Standard);
        Assert.Equal(3, (int)ResponseTier.Full);
    }

    [Fact]
    public void ModelTier_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)ModelTier.Premium);
        Assert.Equal(1, (int)ModelTier.Standard);
        Assert.Equal(2, (int)ModelTier.Fast);
    }

    [Fact]
    public void ModelSelectionConfig_DefaultTier_IsStandard()
    {
        // Arrange & Act
        var config = new ModelSelectionConfig();

        // Assert
        Assert.Equal(ModelTier.Standard, config.DefaultTier);
    }

    [Fact]
    public void ModelSelectionConfig_RespectTierCeiling_DefaultsToTrue()
    {
        // Arrange & Act
        var config = new ModelSelectionConfig();

        // Assert
        Assert.True(config.RespectTierCeiling);
    }
}
