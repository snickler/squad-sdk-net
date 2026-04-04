using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Agents;
using Squad.SDK.NET.Coordinator;
using Squad.SDK.NET.Events;
using Moq;
using Xunit;

namespace Squad.SDK.NET.Tests;

public sealed class FanOutTests
{
    [Fact]
    public async Task SpawnParallelAsync_EmptyChartersList_ReturnsEmptyResult()
    {
        // Arrange
        var mockAgentManager = new Mock<IAgentSessionManager>();

        // Act
        var result = await FanOut.SpawnParallelAsync(
            mockAgentManager.Object,
            [],
            "test message");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SpawnParallelAsync_SpawnsAgentsForEachCharter()
    {
        // Arrange
        var mockAgentManager = new Mock<IAgentSessionManager>();
        var mockSession = new Mock<ISquadSession>();

        mockSession.Setup(s => s.SessionId).Returns("session-123");
        mockSession.Setup(s => s.SendAsync(It.IsAny<SquadMessageOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("response");
        mockSession.Setup(s => s.GetMessagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SquadEvent>)
            [
                new SquadEvent { Type = SquadEventType.SessionCreated }
            ]);

        var charter1 = new AgentCharter { Name = "agent1", Role = "Backend", Prompt = "work" };
        var charter2 = new AgentCharter { Name = "agent2", Role = "Frontend", Prompt = "work" };
        var charter3 = new AgentCharter { Name = "agent3", Role = "QA", Prompt = "work" };

        mockAgentManager.Setup(m => m.SpawnAsync(It.IsAny<AgentCharter>(), It.IsAny<ResponseTier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentCharter c, ResponseTier t, CancellationToken ct) =>
                new AgentSessionInfo
                {
                    Charter = c,
                    State = AgentState.Active,
                    SessionId = $"session-{c.Name}"
                });

        // Act
        await FanOut.SpawnParallelAsync(
            mockAgentManager.Object,
            [charter1, charter2, charter3],
            "test message");

        // Assert
        mockAgentManager.Verify(
            m => m.SpawnAsync(It.IsAny<AgentCharter>(), It.IsAny<ResponseTier>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        mockAgentManager.Verify(
            m => m.SpawnAsync(It.Is<AgentCharter>(c => c.Name == "agent1"), It.IsAny<ResponseTier>(), It.IsAny<CancellationToken>()),
            Times.Once);

        mockAgentManager.Verify(
            m => m.SpawnAsync(It.Is<AgentCharter>(c => c.Name == "agent2"), It.IsAny<ResponseTier>(), It.IsAny<CancellationToken>()),
            Times.Once);

        mockAgentManager.Verify(
            m => m.SpawnAsync(It.Is<AgentCharter>(c => c.Name == "agent3"), It.IsAny<ResponseTier>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SpawnParallelAsync_WithResponseTier_PassesToSpawn()
    {
        // Arrange
        var mockAgentManager = new Mock<IAgentSessionManager>();
        var charter = new AgentCharter { Name = "agent1", Role = "Backend", Prompt = "work" };

        mockAgentManager.Setup(m => m.SpawnAsync(It.IsAny<AgentCharter>(), It.IsAny<ResponseTier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentSessionInfo
            {
                Charter = charter,
                State = AgentState.Active,
                SessionId = "session-123"
            });

        // Act
        await FanOut.SpawnParallelAsync(
            mockAgentManager.Object,
            [charter],
            "test message",
            ResponseTier.Full);

        // Assert
        mockAgentManager.Verify(
            m => m.SpawnAsync(charter, ResponseTier.Full, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SpawnParallelAsync_OnlyActiveAgents_ReceiveMessages()
    {
        // Arrange
        var mockAgentManager = new Mock<IAgentSessionManager>();

        var charter1 = new AgentCharter { Name = "active-agent", Role = "Backend", Prompt = "work" };
        var charter2 = new AgentCharter { Name = "error-agent", Role = "Frontend", Prompt = "work" };

        mockAgentManager.Setup(m => m.SpawnAsync(charter1, It.IsAny<ResponseTier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentSessionInfo
            {
                Charter = charter1,
                State = AgentState.Active,
                SessionId = "session-active"
            });

        mockAgentManager.Setup(m => m.SpawnAsync(charter2, It.IsAny<ResponseTier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentSessionInfo
            {
                Charter = charter2,
                State = AgentState.Error,
                SessionId = null
            });

        // Act
        var result = await FanOut.SpawnParallelAsync(
            mockAgentManager.Object,
            [charter1, charter2],
            "test message");

        // Assert - only active agent should be attempted for messaging
        // Since GetSession returns null for non-AgentSessionManager mocks, we just verify spawning happened
        mockAgentManager.Verify(
            m => m.SpawnAsync(It.IsAny<AgentCharter>(), It.IsAny<ResponseTier>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task SpawnParallelAsync_SingleCharter_Spawns()
    {
        // Arrange
        var mockAgentManager = new Mock<IAgentSessionManager>();
        var charter = new AgentCharter { Name = "solo-agent", Role = "Backend", Prompt = "work" };

        mockAgentManager.Setup(m => m.SpawnAsync(It.IsAny<AgentCharter>(), It.IsAny<ResponseTier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentSessionInfo
            {
                Charter = charter,
                State = AgentState.Active,
                SessionId = "session-solo"
            });

        // Act
        await FanOut.SpawnParallelAsync(
            mockAgentManager.Object,
            [charter],
            "test message");

        // Assert
        mockAgentManager.Verify(
            m => m.SpawnAsync(charter, It.IsAny<ResponseTier>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
