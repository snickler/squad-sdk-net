using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Agents;
using Squad.SDK.NET.Coordinator;
using Squad.SDK.NET.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Squad.SDK.NET.Tests;

public sealed class AgentSessionManagerTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var mockClient = new Mock<ISquadClient>();
        var mockEventBus = new Mock<IEventBus>();

        // Act
        var manager = new AgentSessionManager(mockClient.Object, mockEventBus.Object, NullLogger<AgentSessionManager>.Instance);

        // Assert
        Assert.NotNull(manager);
    }

    [Fact]
    public async Task SpawnAsync_CreatesSessionAndReturnsInfo()
    {
        // Arrange
        var mockClient = new Mock<ISquadClient>();
        var mockEventBus = new Mock<IEventBus>();
        var mockSession = new Mock<ISquadSession>();

        mockSession.Setup(s => s.SessionId).Returns("session-123");
        mockSession.Setup(s => s.SendAsync(It.IsAny<SquadMessageOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("response");

        mockClient.Setup(c => c.CreateSessionAsync(It.IsAny<SquadSessionConfig?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        mockEventBus.Setup(e => e.EmitAsync(It.IsAny<SquadEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var manager = new AgentSessionManager(mockClient.Object, mockEventBus.Object, NullLogger<AgentSessionManager>.Instance);

        var charter = new AgentCharter
        {
            Name = "test-agent",
            Role = "Backend",
            Prompt = "You are a test agent"
        };

        // Act
        var info = await manager.SpawnAsync(charter);

        // Assert
        Assert.NotNull(info);
        Assert.Equal("test-agent", info.Charter.Name);
        Assert.Equal("session-123", info.SessionId);
        Assert.Equal(AgentState.Active, info.State);
    }

    [Fact]
    public async Task GetAgent_AfterSpawn_ReturnsAgent()
    {
        // Arrange
        var mockClient = new Mock<ISquadClient>();
        var mockEventBus = new Mock<IEventBus>();
        var mockSession = new Mock<ISquadSession>();

        mockSession.Setup(s => s.SessionId).Returns("session-123");
        mockClient.Setup(c => c.CreateSessionAsync(It.IsAny<SquadSessionConfig?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);
        mockEventBus.Setup(e => e.EmitAsync(It.IsAny<SquadEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var manager = new AgentSessionManager(mockClient.Object, mockEventBus.Object, NullLogger<AgentSessionManager>.Instance);

        var charter = new AgentCharter
        {
            Name = "test-agent",
            Role = "Backend",
            Prompt = "You are a test agent"
        };

        await manager.SpawnAsync(charter);

        // Act
        var agent = manager.GetAgent("test-agent");

        // Assert
        Assert.NotNull(agent);
        Assert.Equal("test-agent", agent.Charter.Name);
    }

    [Fact]
    public void GetAgent_UnknownName_ReturnsNull()
    {
        // Arrange
        var mockClient = new Mock<ISquadClient>();
        var mockEventBus = new Mock<IEventBus>();
        var manager = new AgentSessionManager(mockClient.Object, mockEventBus.Object, NullLogger<AgentSessionManager>.Instance);

        // Act
        var agent = manager.GetAgent("nonexistent");

        // Assert
        Assert.Null(agent);
    }

    [Fact]
    public async Task GetAllAgents_ReturnsAllSpawnedAgents()
    {
        // Arrange
        var mockClient = new Mock<ISquadClient>();
        var mockEventBus = new Mock<IEventBus>();
        var mockSession = new Mock<ISquadSession>();

        mockSession.Setup(s => s.SessionId).Returns("session-123");
        mockClient.Setup(c => c.CreateSessionAsync(It.IsAny<SquadSessionConfig?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);
        mockEventBus.Setup(e => e.EmitAsync(It.IsAny<SquadEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var manager = new AgentSessionManager(mockClient.Object, mockEventBus.Object, NullLogger<AgentSessionManager>.Instance);

        var charter1 = new AgentCharter { Name = "agent1", Role = "Dev", Prompt = "test" };
        var charter2 = new AgentCharter { Name = "agent2", Role = "QA", Prompt = "test" };
        var charter3 = new AgentCharter { Name = "agent3", Role = "DevOps", Prompt = "test" };

        await manager.SpawnAsync(charter1);
        await manager.SpawnAsync(charter2);
        await manager.SpawnAsync(charter3);

        // Act
        var allAgents = manager.GetAllAgents();

        // Assert
        Assert.Equal(3, allAgents.Count);
        Assert.Contains(allAgents, a => a.Charter.Name == "agent1");
        Assert.Contains(allAgents, a => a.Charter.Name == "agent2");
        Assert.Contains(allAgents, a => a.Charter.Name == "agent3");
    }

    [Fact]
    public async Task DestroyAsync_RemovesAgent()
    {
        // Arrange
        var mockClient = new Mock<ISquadClient>();
        var mockEventBus = new Mock<IEventBus>();
        var mockSession = new Mock<ISquadSession>();

        mockSession.Setup(s => s.SessionId).Returns("session-123");
        mockSession.Setup(s => s.DisposeAsync()).Returns(ValueTask.CompletedTask);

        mockClient.Setup(c => c.CreateSessionAsync(It.IsAny<SquadSessionConfig?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);
        mockEventBus.Setup(e => e.EmitAsync(It.IsAny<SquadEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var manager = new AgentSessionManager(mockClient.Object, mockEventBus.Object, NullLogger<AgentSessionManager>.Instance);

        var charter = new AgentCharter { Name = "test-agent", Role = "Backend", Prompt = "test" };
        await manager.SpawnAsync(charter);

        // Act
        await manager.DestroyAsync("test-agent");
        var agent = manager.GetAgent("test-agent");

        // Assert
        Assert.Null(agent);
        mockSession.Verify(s => s.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task SpawnAsync_SetsStateToSpawning_ThenActive()
    {
        // Arrange
        var mockClient = new Mock<ISquadClient>();
        var mockEventBus = new Mock<IEventBus>();
        var mockSession = new Mock<ISquadSession>();

        mockSession.Setup(s => s.SessionId).Returns("session-123");
        mockClient.Setup(c => c.CreateSessionAsync(It.IsAny<SquadSessionConfig?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        var stateEvents = new List<AgentState>();
        mockEventBus.Setup(e => e.EmitAsync(It.IsAny<SquadEvent>(), It.IsAny<CancellationToken>()))
            .Callback<SquadEvent, CancellationToken>((evt, ct) =>
            {
                if (evt.Payload is AgentState state)
                {
                    stateEvents.Add(state);
                }
            })
            .Returns(Task.CompletedTask);

        var manager = new AgentSessionManager(mockClient.Object, mockEventBus.Object, NullLogger<AgentSessionManager>.Instance);

        var charter = new AgentCharter { Name = "test-agent", Role = "Backend", Prompt = "test" };

        // Act
        var info = await manager.SpawnAsync(charter);

        // Assert
        Assert.Equal(AgentState.Active, info.State);
        Assert.Contains(AgentState.Spawning, stateEvents);
        Assert.Contains(AgentState.Active, stateEvents);
    }

    [Fact]
    public async Task SpawnAsync_OnError_SetsStateToError()
    {
        // Arrange
        var mockClient = new Mock<ISquadClient>();
        var mockEventBus = new Mock<IEventBus>();

        mockClient.Setup(c => c.CreateSessionAsync(It.IsAny<SquadSessionConfig?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection failed"));

        mockEventBus.Setup(e => e.EmitAsync(It.IsAny<SquadEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var manager = new AgentSessionManager(mockClient.Object, mockEventBus.Object, NullLogger<AgentSessionManager>.Instance);

        var charter = new AgentCharter { Name = "test-agent", Role = "Backend", Prompt = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await manager.SpawnAsync(charter));

        var agent = manager.GetAgent("test-agent");
        Assert.NotNull(agent);
        Assert.Equal(AgentState.Error, agent.State);
    }

    [Fact]
    public async Task SpawnAsync_WithResponseTier_ConfiguresCorrectly()
    {
        // Arrange
        var mockClient = new Mock<ISquadClient>();
        var mockEventBus = new Mock<IEventBus>();
        var mockSession = new Mock<ISquadSession>();

        mockSession.Setup(s => s.SessionId).Returns("session-123");
        mockClient.Setup(c => c.CreateSessionAsync(It.IsAny<SquadSessionConfig?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);
        mockEventBus.Setup(e => e.EmitAsync(It.IsAny<SquadEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var manager = new AgentSessionManager(mockClient.Object, mockEventBus.Object, NullLogger<AgentSessionManager>.Instance);

        var charter = new AgentCharter { Name = "test-agent", Role = "Backend", Prompt = "test" };

        // Act
        var info = await manager.SpawnAsync(charter, ResponseTier.Full);

        // Assert
        Assert.Equal(ResponseTier.Full, info.ResponseMode);
    }
}
