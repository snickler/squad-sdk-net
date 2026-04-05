using Squad.SDK.NET.Tools;
using Xunit;

namespace Squad.SDK.NET.Tests;

public class BuiltInToolsTests
{
    [Fact]
    public void SquadToolResult_Ok_CreatesSuccessResult()
    {
        // Act
        var result = SquadToolResult.Ok("Operation successful");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Operation successful", result.Message);
    }

    [Fact]
    public void SquadToolResult_Ok_WithData_IncludesData()
    {
        // Arrange
        var data = new { Count = 42, Name = "Test" };

        // Act
        var result = SquadToolResult.Ok("Success with data", data);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Success with data", result.Message);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public void SquadToolResult_Fail_CreatesFailureResult()
    {
        // Act
        var result = SquadToolResult.Fail("Operation failed");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Operation failed", result.Message);
        Assert.Null(result.Data);
    }
}
