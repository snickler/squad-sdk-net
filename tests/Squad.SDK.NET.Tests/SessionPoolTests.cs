using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Runtime;

namespace Squad.SDK.NET.Tests;

public sealed class SessionPoolTests
{
    [Fact]
    public void Add_AndGet_Session()
    {
        // Arrange
        var pool = new SessionPool();
        var mockSession = new Mock<ISquadSession>();
        mockSession.Setup(s => s.SessionId).Returns("session1");

        // Act
        pool.Add(mockSession.Object);
        var retrieved = pool.Get("session1");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("session1", retrieved.SessionId);
    }

    [Fact]
    public void Remove_Session()
    {
        // Arrange
        var pool = new SessionPool();
        var mockSession = new Mock<ISquadSession>();
        mockSession.Setup(s => s.SessionId).Returns("session1");
        pool.Add(mockSession.Object);

        // Act
        pool.Remove("session1");
        var retrieved = pool.Get("session1");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public void Get_UnknownSession_ReturnsNull()
    {
        // Arrange
        var pool = new SessionPool();

        // Act
        var retrieved = pool.Get("unknown-session");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public void GetAll_ReturnsAllSessions()
    {
        // Arrange
        var pool = new SessionPool();
        var mockSession1 = new Mock<ISquadSession>();
        mockSession1.Setup(s => s.SessionId).Returns("session1");
        var mockSession2 = new Mock<ISquadSession>();
        mockSession2.Setup(s => s.SessionId).Returns("session2");

        // Act
        pool.Add(mockSession1.Object);
        pool.Add(mockSession2.Object);
        var all = pool.GetAll();

        // Assert
        Assert.Equal(2, all.Count);
        Assert.Contains(all, s => s.SessionId == "session1");
        Assert.Contains(all, s => s.SessionId == "session2");
    }

    [Fact]
    public async Task ShutdownAsync_DisposesAllSessions()
    {
        // Arrange
        var pool = new SessionPool();
        var mockSession1 = new Mock<ISquadSession>();
        mockSession1.Setup(s => s.SessionId).Returns("session1");
        mockSession1.Setup(s => s.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var mockSession2 = new Mock<ISquadSession>();
        mockSession2.Setup(s => s.SessionId).Returns("session2");
        mockSession2.Setup(s => s.DisposeAsync()).Returns(ValueTask.CompletedTask);

        pool.Add(mockSession1.Object);
        pool.Add(mockSession2.Object);

        // Act
        await pool.ShutdownAsync();

        // Assert
        mockSession1.Verify(s => s.DisposeAsync(), Times.Once);
        mockSession2.Verify(s => s.DisposeAsync(), Times.Once);
        Assert.Empty(pool.GetAll());
    }

    [Fact]
    public void Add_DuplicateSessionId_Overwrites()
    {
        // Arrange
        var pool = new SessionPool();
        var mockSession1 = new Mock<ISquadSession>();
        mockSession1.Setup(s => s.SessionId).Returns("session1");
        var mockSession2 = new Mock<ISquadSession>();
        mockSession2.Setup(s => s.SessionId).Returns("session1");

        // Act
        pool.Add(mockSession1.Object);
        pool.Add(mockSession2.Object);
        var retrieved = pool.Get("session1");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Same(mockSession2.Object, retrieved);
    }

    [Fact]
    public void GetAll_EmptyPool_ReturnsEmptyList()
    {
        // Arrange
        var pool = new SessionPool();

        // Act
        var all = pool.GetAll();

        // Assert
        Assert.NotNull(all);
        Assert.Empty(all);
    }
}
