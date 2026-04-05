using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Agents;
using Squad.SDK.NET.Config;
using Squad.SDK.NET.Coordinator;
using Squad.SDK.NET.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using CoordinatorClass = Squad.SDK.NET.Coordinator.Coordinator;

namespace Squad.SDK.NET.Tests;

public sealed class CoordinatorTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var config = new SquadConfig { Team = new TeamConfig { Name = "Test" } };
        var mockAgentManager = new Mock<IAgentSessionManager>();
        var mockEventBus = new Mock<IEventBus>();

        // Act
        var coordinator = new CoordinatorClass(config, mockAgentManager.Object, mockEventBus.Object, NullLogger<CoordinatorClass>.Instance);

        // Assert
        Assert.NotNull(coordinator);
    }

    [Fact]
    public async Task InitializeAsync_DoesNotThrow()
    {
        // Arrange
        var config = new SquadConfig { Team = new TeamConfig { Name = "Test" } };
        var mockAgentManager = new Mock<IAgentSessionManager>();
        var mockEventBus = new Mock<IEventBus>();
        var coordinator = new CoordinatorClass(config, mockAgentManager.Object, mockEventBus.Object, NullLogger<CoordinatorClass>.Instance);

        // Act & Assert
        await coordinator.InitializeAsync();
    }

    [Fact]
    public async Task ShutdownAsync_DoesNotThrow()
    {
        // Arrange
        var config = new SquadConfig { Team = new TeamConfig { Name = "Test" } };
        var mockAgentManager = new Mock<IAgentSessionManager>();
        var mockEventBus = new Mock<IEventBus>();
        var coordinator = new CoordinatorClass(config, mockAgentManager.Object, mockEventBus.Object, NullLogger<CoordinatorClass>.Instance);

        // Act & Assert
        await coordinator.ShutdownAsync();
    }

    [Fact]
    public async Task RouteAsync_WithMatchingRule_ReturnsCorrectDecision()
    {
        // Arrange
        var config = new SquadConfig
        {
            Team = new TeamConfig { Name = "Test" },
            Routing = new RoutingConfig
            {
                Rules =
                [
                    new RoutingRule
                    {
                        WorkType = "backend-api",
                        Agents = ["agent1"],
                        Tier = ResponseTier.Standard,
                        Priority = 10
                    }
                ]
            }
        };

        var mockAgentManager = new Mock<IAgentSessionManager>();
        var mockEventBus = new Mock<IEventBus>();
        mockEventBus.Setup(e => e.EmitAsync(It.IsAny<SquadEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var coordinator = new CoordinatorClass(config, mockAgentManager.Object, mockEventBus.Object, NullLogger<CoordinatorClass>.Instance);
        await coordinator.InitializeAsync();

        // Act
        var decision = await coordinator.RouteAsync("I need help with backend api development");

        // Assert
        Assert.Equal(ResponseTier.Standard, decision.Tier);
        Assert.Single(decision.Agents);
        Assert.Contains("agent1", decision.Agents);
        Assert.False(decision.Parallel);
        Assert.Contains("backend-api", decision.Rationale);
    }

    [Fact]
    public async Task RouteAsync_WithMultipleAgents_SetsParallelTrue()
    {
        // Arrange
        var config = new SquadConfig
        {
            Team = new TeamConfig { Name = "Test" },
            Routing = new RoutingConfig
            {
                Rules =
                [
                    new RoutingRule
                    {
                        WorkType = "full-stack",
                        Agents = ["agent1", "agent2", "agent3"],
                        Tier = ResponseTier.Full,
                        Priority = 10
                    }
                ]
            }
        };

        var mockAgentManager = new Mock<IAgentSessionManager>();
        var mockEventBus = new Mock<IEventBus>();
        mockEventBus.Setup(e => e.EmitAsync(It.IsAny<SquadEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var coordinator = new CoordinatorClass(config, mockAgentManager.Object, mockEventBus.Object, NullLogger<CoordinatorClass>.Instance);
        await coordinator.InitializeAsync();

        // Act
        var decision = await coordinator.RouteAsync("I need full stack help");

        // Assert
        Assert.True(decision.Parallel);
        Assert.Equal(3, decision.Agents.Count);
    }

    [Fact]
    public async Task RouteAsync_NoMatchWithDefaultAgent_ReturnsFallbackDecision()
    {
        // Arrange
        var config = new SquadConfig
        {
            Team = new TeamConfig { Name = "Test" },
            Routing = new RoutingConfig
            {
                DefaultAgent = "default-agent",
                FallbackBehavior = RoutingFallbackBehavior.DefaultAgent
            }
        };

        var mockAgentManager = new Mock<IAgentSessionManager>();
        var mockEventBus = new Mock<IEventBus>();
        mockEventBus.Setup(e => e.EmitAsync(It.IsAny<SquadEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var coordinator = new CoordinatorClass(config, mockAgentManager.Object, mockEventBus.Object, NullLogger<CoordinatorClass>.Instance);
        await coordinator.InitializeAsync();

        // Act
        var decision = await coordinator.RouteAsync("some random query");

        // Assert
        Assert.Single(decision.Agents);
        Assert.Contains("default-agent", decision.Agents);
        Assert.False(decision.Parallel);
        Assert.Contains("default agent", decision.Rationale, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RouteAsync_NoMatchCoordinatorFallback_ReturnsAllActiveAgents()
    {
        // Arrange
        var config = new SquadConfig
        {
            Team = new TeamConfig { Name = "Test" },
            Routing = new RoutingConfig
            {
                FallbackBehavior = RoutingFallbackBehavior.Coordinator
            }
        };

        var mockAgentManager = new Mock<IAgentSessionManager>();
        mockAgentManager.Setup(m => m.GetAllAgents()).Returns(
        [
            new AgentSessionInfo
            {
                Charter = new AgentCharter { Name = "agent1", Role = "Dev", Prompt = "test" },
                State = AgentState.Active
            },
            new AgentSessionInfo
            {
                Charter = new AgentCharter { Name = "agent2", Role = "QA", Prompt = "test" },
                State = AgentState.Active
            }
        ]);

        var mockEventBus = new Mock<IEventBus>();
        mockEventBus.Setup(e => e.EmitAsync(It.IsAny<SquadEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var coordinator = new CoordinatorClass(config, mockAgentManager.Object, mockEventBus.Object, NullLogger<CoordinatorClass>.Instance);
        await coordinator.InitializeAsync();

        // Act
        var decision = await coordinator.RouteAsync("some random query");

        // Assert
        Assert.True(decision.Parallel);
        Assert.Equal(2, decision.Agents.Count);
        Assert.Contains("coordinator", decision.Rationale, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithDecision_CallsAgentManager()
    {
        // Arrange
        var config = new SquadConfig { Team = new TeamConfig { Name = "Test" } };
        var mockAgentManager = new Mock<IAgentSessionManager>();
        var mockEventBus = new Mock<IEventBus>();
        var coordinator = new CoordinatorClass(config, mockAgentManager.Object, mockEventBus.Object, NullLogger<CoordinatorClass>.Instance);

        var decision = new RoutingDecision
        {
            Tier = ResponseTier.Standard,
            Agents = ["test-agent"],
            Parallel = false
        };

        mockAgentManager.Setup(m => m.GetAgent("test-agent")).Returns(
            new AgentSessionInfo
            {
                Charter = new AgentCharter { Name = "test-agent", Role = "Dev", Prompt = "test" },
                State = AgentState.Active,
                SessionId = "session-123"
            });

        // Act & Assert - just verify no exception is thrown
        await coordinator.ExecuteAsync(decision, "test message");
    }
}

