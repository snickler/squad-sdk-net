using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Agents;
using Squad.SDK.NET.Coordinator;
using Squad.SDK.NET.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Squad.SDK.NET.Tests;

public sealed class SubAgentTests
{
    [Fact]
    public async Task SpawnSubAgentAsync_ValidatesParentChildRelationship()
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

        var parentCharter = new AgentCharter
        {
            Name = "parent-agent",
            Role = "Coordinator",
            Prompt = "You are a parent agent"
        };

        var childCharter = new AgentCharter
        {
            Name = "child-agent",
            Role = "Worker",
            Prompt = "You are a child agent"
        };

        // Spawn parent
        var parent = await manager.SpawnAsync(parentCharter);

        // Act
        var child = await manager.SpawnSubAgentAsync("parent-agent", childCharter);

        // Assert
        Assert.NotNull(child);
        Assert.Equal("child-agent", child.Charter.Name);
        Assert.Equal("parent-agent", child.ParentAgentName);
        Assert.Equal(1, child.Depth);
        Assert.Contains("child-agent", parent.SubAgentNames);
    }

    [Fact]
    public async Task SpawnSubAgentAsync_ValidatesDepthIncrement()
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

        var parentCharter = new AgentCharter
        {
            Name = "level-0",
            Role = "Root",
            Prompt = "You are a root agent"
        };

        var level1Charter = new AgentCharter
        {
            Name = "level-1",
            Role = "Worker",
            Prompt = "You are level 1"
        };

        var level2Charter = new AgentCharter
        {
            Name = "level-2",
            Role = "Worker",
            Prompt = "You are level 2"
        };

        // Act
        var level0 = await manager.SpawnAsync(parentCharter);
        var level1 = await manager.SpawnSubAgentAsync("level-0", level1Charter);
        var level2 = await manager.SpawnSubAgentAsync("level-1", level2Charter);

        // Assert
        Assert.Equal(0, level0.Depth);
        Assert.Equal(1, level1.Depth);
        Assert.Equal(2, level2.Depth);
    }

    [Fact]
    public async Task SpawnSubAgentAsync_ThrowsWhenParentNotFound()
    {
        // Arrange
        var mockClient = new Mock<ISquadClient>();
        var mockEventBus = new Mock<IEventBus>();
        var manager = new AgentSessionManager(mockClient.Object, mockEventBus.Object, NullLogger<AgentSessionManager>.Instance);

        var childCharter = new AgentCharter
        {
            Name = "child-agent",
            Role = "Worker",
            Prompt = "You are a child agent"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.SpawnSubAgentAsync("nonexistent-parent", childCharter));
        
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task SpawnSubAgentAsync_ThrowsWhenParentNotActive()
    {
        // Arrange
        var mockClient = new Mock<ISquadClient>();
        var mockEventBus = new Mock<IEventBus>();
        var mockSession = new Mock<ISquadSession>();

        mockSession.Setup(s => s.SessionId).Returns("session-123");
        mockClient.Setup(c => c.CreateSessionAsync(It.IsAny<SquadSessionConfig?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection failed"));
        mockEventBus.Setup(e => e.EmitAsync(It.IsAny<SquadEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var manager = new AgentSessionManager(mockClient.Object, mockEventBus.Object, NullLogger<AgentSessionManager>.Instance);

        var parentCharter = new AgentCharter
        {
            Name = "parent-agent",
            Role = "Coordinator",
            Prompt = "You are a parent agent"
        };

        var childCharter = new AgentCharter
        {
            Name = "child-agent",
            Role = "Worker",
            Prompt = "You are a child agent"
        };

        // Try to spawn parent (will fail)
        try
        {
            await manager.SpawnAsync(parentCharter);
        }
        catch
        {
            // Expected to fail
        }

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.SpawnSubAgentAsync("parent-agent", childCharter));
        
        Assert.Contains("not active", exception.Message);
    }

    [Fact]
    public async Task SpawnSubAgentAsync_ThrowsWhenMaxDepthExceeded()
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

        // Create 4 levels of agents (0, 1, 2, 3)
        var level0 = await manager.SpawnAsync(new AgentCharter
        {
            Name = "level-0",
            Role = "Root",
            Prompt = "Level 0"
        });

        var level1 = await manager.SpawnSubAgentAsync("level-0", new AgentCharter
        {
            Name = "level-1",
            Role = "Worker",
            Prompt = "Level 1"
        });

        var level2 = await manager.SpawnSubAgentAsync("level-1", new AgentCharter
        {
            Name = "level-2",
            Role = "Worker",
            Prompt = "Level 2"
        });

        var level3 = await manager.SpawnSubAgentAsync("level-2", new AgentCharter
        {
            Name = "level-3",
            Role = "Worker",
            Prompt = "Level 3"
        });

        // Act & Assert - Try to spawn level 4 (should fail)
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.SpawnSubAgentAsync("level-3", new AgentCharter
            {
                Name = "level-4",
                Role = "Worker",
                Prompt = "Level 4"
            }));
        
        Assert.Contains("Maximum sub-agent depth", exception.Message);
    }

    [Fact]
    public async Task GetSubAgents_ReturnsCorrectChildren()
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

        var parent = await manager.SpawnAsync(new AgentCharter
        {
            Name = "parent",
            Role = "Coordinator",
            Prompt = "Parent"
        });

        await manager.SpawnSubAgentAsync("parent", new AgentCharter
        {
            Name = "child1",
            Role = "Worker",
            Prompt = "Child 1"
        });

        await manager.SpawnSubAgentAsync("parent", new AgentCharter
        {
            Name = "child2",
            Role = "Worker",
            Prompt = "Child 2"
        });

        // Act
        var children = manager.GetSubAgents("parent");

        // Assert
        Assert.Equal(2, children.Count);
        Assert.Contains(children, c => c.Charter.Name == "child1");
        Assert.Contains(children, c => c.Charter.Name == "child2");
    }

    [Fact]
    public async Task GetAgentTree_ReturnsFullTree()
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

        // Create tree: root -> child1, child2 -> grandchild1
        await manager.SpawnAsync(new AgentCharter
        {
            Name = "root",
            Role = "Coordinator",
            Prompt = "Root"
        });

        await manager.SpawnSubAgentAsync("root", new AgentCharter
        {
            Name = "child1",
            Role = "Worker",
            Prompt = "Child 1"
        });

        await manager.SpawnSubAgentAsync("root", new AgentCharter
        {
            Name = "child2",
            Role = "Worker",
            Prompt = "Child 2"
        });

        await manager.SpawnSubAgentAsync("child1", new AgentCharter
        {
            Name = "grandchild1",
            Role = "Worker",
            Prompt = "Grandchild 1"
        });

        // Act
        var tree = manager.GetAgentTree("root");

        // Assert
        Assert.Equal(4, tree.Count);
        Assert.Contains(tree, a => a.Charter.Name == "root");
        Assert.Contains(tree, a => a.Charter.Name == "child1");
        Assert.Contains(tree, a => a.Charter.Name == "child2");
        Assert.Contains(tree, a => a.Charter.Name == "grandchild1");
    }

    [Fact]
    public async Task DestroyAsync_CascadesToSubAgents()
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

        await manager.SpawnAsync(new AgentCharter
        {
            Name = "parent",
            Role = "Coordinator",
            Prompt = "Parent"
        });

        await manager.SpawnSubAgentAsync("parent", new AgentCharter
        {
            Name = "child1",
            Role = "Worker",
            Prompt = "Child 1"
        });

        await manager.SpawnSubAgentAsync("parent", new AgentCharter
        {
            Name = "child2",
            Role = "Worker",
            Prompt = "Child 2"
        });

        // Act
        await manager.DestroyAsync("parent");

        // Assert - All agents should be destroyed
        Assert.Null(manager.GetAgent("parent"));
        Assert.Null(manager.GetAgent("child1"));
        Assert.Null(manager.GetAgent("child2"));
    }

    [Fact]
    public async Task DestroyAsync_RemovesFromParentSubAgentNames()
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

        var parent = await manager.SpawnAsync(new AgentCharter
        {
            Name = "parent",
            Role = "Coordinator",
            Prompt = "Parent"
        });

        await manager.SpawnSubAgentAsync("parent", new AgentCharter
        {
            Name = "child1",
            Role = "Worker",
            Prompt = "Child 1"
        });

        await manager.SpawnSubAgentAsync("parent", new AgentCharter
        {
            Name = "child2",
            Role = "Worker",
            Prompt = "Child 2"
        });

        // Act - Destroy only child1
        await manager.DestroyAsync("child1");

        // Assert
        Assert.Null(manager.GetAgent("child1"));
        Assert.NotNull(manager.GetAgent("parent"));
        Assert.NotNull(manager.GetAgent("child2"));
        Assert.DoesNotContain("child1", parent.SubAgentNames);
        Assert.Contains("child2", parent.SubAgentNames);
    }

    [Fact]
    public async Task SpawnSubAgentAsync_EmitsSubAgentSpawnedEvent()
    {
        // Arrange
        var mockClient = new Mock<ISquadClient>();
        var mockEventBus = new Mock<IEventBus>();
        var mockSession = new Mock<ISquadSession>();

        mockSession.Setup(s => s.SessionId).Returns("session-123");
        mockClient.Setup(c => c.CreateSessionAsync(It.IsAny<SquadSessionConfig?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        SquadEvent? subAgentEvent = null;
        mockEventBus.Setup(e => e.EmitAsync(It.IsAny<SquadEvent>(), It.IsAny<CancellationToken>()))
            .Callback<SquadEvent, CancellationToken>((e, ct) =>
            {
                if (e.Payload is SubAgentSpawnedPayload)
                {
                    subAgentEvent = e;
                }
            })
            .Returns(Task.CompletedTask);

        var manager = new AgentSessionManager(mockClient.Object, mockEventBus.Object, NullLogger<AgentSessionManager>.Instance);

        await manager.SpawnAsync(new AgentCharter
        {
            Name = "parent",
            Role = "Coordinator",
            Prompt = "Parent"
        });

        // Act
        await manager.SpawnSubAgentAsync("parent", new AgentCharter
        {
            Name = "child",
            Role = "Worker",
            Prompt = "Child"
        });

        // Assert
        Assert.NotNull(subAgentEvent);
        Assert.Equal(SquadEventType.AgentMilestone, subAgentEvent.Type);
        Assert.Equal("child", subAgentEvent.AgentName);
        
        var payload = Assert.IsType<SubAgentSpawnedPayload>(subAgentEvent.Payload);
        Assert.Equal("parent", payload.ParentAgentName);
        Assert.Equal("child", payload.ChildAgentName);
        Assert.Equal(1, payload.Depth);
    }
}
