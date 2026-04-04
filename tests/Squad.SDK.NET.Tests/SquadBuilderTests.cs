using Squad.SDK.NET.Builder;
using Squad.SDK.NET.Config;
using Squad.SDK.NET.Hooks;
using Xunit;

namespace Squad.SDK.NET.Tests;

public sealed class SquadBuilderTests
{
    [Fact]
    public void Create_ReturnsNonNullBuilder()
    {
        // Act
        var builder = SquadBuilder.Create();

        // Assert
        Assert.NotNull(builder);
    }

    [Fact]
    public void WithTeam_ConfiguresTeam()
    {
        // Arrange
        var builder = SquadBuilder.Create();

        // Act
        builder.WithTeam(t => t.Name("MyTeam").Description("Test team description"));
        var config = builder.Build();

        // Assert
        Assert.Equal("MyTeam", config.Team.Name);
        Assert.Equal("Test team description", config.Team.Description);
    }

    [Fact]
    public void WithAgent_AddsAgent()
    {
        // Arrange
        var builder = SquadBuilder.Create();

        // Act
        builder
            .WithTeam(t => t.Name("MyTeam"))
            .WithAgent(a => a.Name("agent1").Role("dev").Prompt("do work"));
        var config = builder.Build();

        // Assert
        Assert.Single(config.Agents);
        Assert.Equal("agent1", config.Agents[0].Name);
        Assert.Equal("dev", config.Agents[0].Role);
        Assert.Equal("do work", config.Agents[0].Prompt);
    }

    [Fact]
    public void WithAgent_MultipleCalls_AddsMultipleAgents()
    {
        // Arrange
        var builder = SquadBuilder.Create();

        // Act
        builder
            .WithTeam(t => t.Name("MyTeam"))
            .WithAgent(a => a.Name("agent1").Role("backend").Prompt("backend work"))
            .WithAgent(a => a.Name("agent2").Role("frontend").Prompt("frontend work"))
            .WithAgent(a => a.Name("agent3").Role("qa").Prompt("testing work"));
        var config = builder.Build();

        // Assert
        Assert.Equal(3, config.Agents.Count);
        Assert.Contains(config.Agents, a => a.Name == "agent1");
        Assert.Contains(config.Agents, a => a.Name == "agent2");
        Assert.Contains(config.Agents, a => a.Name == "agent3");
    }

    [Fact]
    public void WithRouting_SetsRoutingConfig()
    {
        // Arrange
        var builder = SquadBuilder.Create();

        // Act
        builder
            .WithTeam(t => t.Name("MyTeam"))
            .WithRouting(r => r.DefaultAgent("agent1"));
        var config = builder.Build();

        // Assert
        Assert.NotNull(config.Routing);
        Assert.Equal("agent1", config.Routing.DefaultAgent);
    }

    [Fact]
    public void WithModels_SetsModelConfig()
    {
        // Arrange
        var builder = SquadBuilder.Create();

        // Act
        builder
            .WithTeam(t => t.Name("MyTeam"))
            .WithModels(m => m.Default("gpt-5"));
        var config = builder.Build();

        // Assert
        Assert.NotNull(config.Models);
        Assert.Equal("gpt-5", config.Models.DefaultModel);
    }

    [Fact]
    public void WithHooks_SetsPolicyConfig()
    {
        // Arrange
        var builder = SquadBuilder.Create();
        var policy = new PolicyConfig
        {
            MaxAskUserPerSession = 5,
            ScrubPii = true
        };

        // Act
        builder
            .WithTeam(t => t.Name("MyTeam"))
            .WithHooks(policy);
        var config = builder.Build();

        // Assert - config doesn't expose hooks/policy directly, but call shouldn't throw
        Assert.NotNull(config);
    }

    [Fact]
    public void Build_WithOnlyTeam_ProducesMinimalConfig()
    {
        // Arrange
        var builder = SquadBuilder.Create();

        // Act
        builder.WithTeam(t => t.Name("MinimalTeam"));
        var config = builder.Build();

        // Assert
        Assert.Equal("MinimalTeam", config.Team.Name);
        Assert.Empty(config.Agents);
        Assert.Null(config.Routing);
        Assert.Null(config.Models);
    }

    [Fact]
    public void Build_WithoutTeam_ThrowsException()
    {
        // Arrange
        var builder = SquadBuilder.Create();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("team", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_ProducesConfigWithCorrectVersion()
    {
        // Arrange
        var builder = SquadBuilder.Create();

        // Act
        builder.WithTeam(t => t.Name("MyTeam"));
        var config = builder.Build();

        // Assert
        Assert.Equal("1.0", config.Version);
    }

    [Fact]
    public void FluentAPI_Chainable()
    {
        // Arrange & Act
        var config = SquadBuilder.Create()
            .WithTeam(t => t.Name("ChainTeam").Description("Fluent test"))
            .WithAgent(a => a.Name("agent1").Role("dev").Prompt("work"))
            .WithAgent(a => a.Name("agent2").Role("qa").Prompt("test"))
            .WithRouting(r => r.DefaultAgent("agent1"))
            .WithModels(m => m.Default("gpt-5"))
            .Build();

        // Assert
        Assert.Equal("ChainTeam", config.Team.Name);
        Assert.Equal(2, config.Agents.Count);
        Assert.NotNull(config.Routing);
        Assert.NotNull(config.Models);
    }
}
