using Squad.SDK.NET.Coordinator;
using Xunit;

namespace Squad.SDK.NET.Tests;

public class DirectResponseTests
{
    [Theory]
    [InlineData("what is the status?")]
    [InlineData("show me status")]
    [InlineData("status check")]
    [InlineData("what's the current status")]
    public void TryGetDirectResponse_StatusQuery_ReturnsTrue(string message)
    {
        // Act
        var result = DirectResponse.TryGetDirectResponse(message, out var response);

        // Assert
        Assert.True(result);
        Assert.NotNull(response);
        Assert.NotEmpty(response);
    }

    [Theory]
    [InlineData("implement authentication")]
    [InlineData("write tests for the API")]
    [InlineData("deploy to production")]
    [InlineData("refactor the codebase")]
    public void TryGetDirectResponse_WorkRequest_ReturnsFalse(string message)
    {
        // Act
        var result = DirectResponse.TryGetDirectResponse(message, out var response);

        // Assert
        Assert.False(result);
        Assert.Null(response);
    }

    [Fact]
    public void TryGetDirectResponse_StatusQuery_ContainsMeaningfulContent()
    {
        // Arrange
        var message = "what is the status?";

        // Act
        var result = DirectResponse.TryGetDirectResponse(message, out var response);

        // Assert
        Assert.True(result);
        Assert.NotNull(response);
        Assert.True(response.Length > 10, "Response should contain meaningful content");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryGetDirectResponse_EmptyMessage_ReturnsFalse(string? message)
    {
        // Act
        var result = DirectResponse.TryGetDirectResponse(message!, out var response);

        // Assert
        Assert.False(result);
    }
}
