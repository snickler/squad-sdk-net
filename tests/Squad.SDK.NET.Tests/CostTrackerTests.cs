using Squad.SDK.NET.Runtime;

namespace Squad.SDK.NET.Tests;

public sealed class CostTrackerTests
{
    [Fact]
    public void RecordUsage_UpdatesTotals()
    {
        // Arrange
        var tracker = new CostTracker();

        // Act
        tracker.RecordUsage(Constants.Models.Gpt5, "session1", 1000, 500);

        // Assert
        var modelUsage = tracker.GetModelUsage(Constants.Models.Gpt5);
        Assert.Equal(1000, modelUsage.TotalInputTokens);
        Assert.Equal(500, modelUsage.TotalOutputTokens);
        Assert.Equal(1, modelUsage.RequestCount);
    }

    [Theory]
    [InlineData(Constants.Models.Gpt5, 1000, 500, 0.0075)]
    [InlineData(Constants.Models.Gpt5Mini, 1000, 500, 0.0012)]
    [InlineData(Constants.Models.ClaudeOpus, 1000, 500, 0.0525)]
    [InlineData(Constants.Models.ClaudeSonnet, 1000, 500, 0.0105)]
    [InlineData(Constants.Models.ClaudeHaiku, 1000, 500, 0.0028)]
    public void EstimateCost_ReturnsCorrectValues_ForKnownModels(
        string model, int inputTokens, int outputTokens, decimal expectedCost)
    {
        // Arrange
        var tracker = new CostTracker();

        // Act
        var cost = tracker.EstimateCost(model, inputTokens, outputTokens);

        // Assert
        Assert.Equal(expectedCost, cost);
    }

    [Fact]
    public void EstimateCost_UnknownModel_ReturnsZero()
    {
        // Arrange
        var tracker = new CostTracker();

        // Act
        var cost = tracker.EstimateCost("unknown-model", 1000, 500);

        // Assert
        Assert.Equal(0m, cost);
    }

    [Fact]
    public void GetModelUsage_ReturnsCorrectAggregates()
    {
        // Arrange
        var tracker = new CostTracker();

        // Act
        tracker.RecordUsage(Constants.Models.Gpt5, "session1", 1000, 500);
        tracker.RecordUsage(Constants.Models.Gpt5, "session2", 2000, 1000);
        tracker.RecordUsage(Constants.Models.Gpt5, "session3", 500, 250);

        // Assert
        var usage = tracker.GetModelUsage(Constants.Models.Gpt5);
        Assert.Equal(3500, usage.TotalInputTokens);
        Assert.Equal(1750, usage.TotalOutputTokens);
        Assert.Equal(3, usage.RequestCount);
        Assert.Equal(0.02625m, usage.EstimatedCost); // (3500*2.5 + 1750*10) / 1_000_000
    }

    [Fact]
    public void GetSessionUsage_ReturnsCorrectAggregates()
    {
        // Arrange
        var tracker = new CostTracker();

        // Act
        tracker.RecordUsage(Constants.Models.Gpt5, "session1", 1000, 500);
        tracker.RecordUsage(Constants.Models.Gpt5Mini, "session1", 2000, 1000);
        tracker.RecordUsage(Constants.Models.ClaudeHaiku, "session1", 500, 250);

        // Assert
        var usage = tracker.GetSessionUsage("session1");
        Assert.Equal(3500, usage.TotalInputTokens);
        Assert.Equal(1750, usage.TotalOutputTokens);
        // (1000*2.5 + 500*10 + 2000*0.4 + 1000*1.6 + 500*0.8 + 250*4) / 1_000_000
        // = (2500 + 5000 + 800 + 1600 + 400 + 1000) / 1_000_000 = 0.0113
        Assert.Equal(0.0113m, usage.EstimatedCost);
    }

    [Fact]
    public void GetTotalSummary_IncludesAllModelsAndSessions()
    {
        // Arrange
        var tracker = new CostTracker();

        // Act
        tracker.RecordUsage(Constants.Models.Gpt5, "session1", 1000, 500);
        tracker.RecordUsage(Constants.Models.Gpt5Mini, "session1", 1000, 500);
        tracker.RecordUsage(Constants.Models.Gpt5, "session2", 2000, 1000);

        // Assert
        var summary = tracker.GetTotalSummary();
        Assert.Equal(4000, summary.TotalInputTokens);
        Assert.Equal(2000, summary.TotalOutputTokens);
        Assert.Equal(2, summary.ByModel.Count);
        Assert.Equal(2, summary.BySession.Count);
        Assert.True(summary.TotalEstimatedCost > 0);
    }

    [Fact]
    public void RecordUsage_MultipleSessionsTrackedIndependently()
    {
        // Arrange
        var tracker = new CostTracker();

        // Act
        tracker.RecordUsage(Constants.Models.Gpt5, "session1", 1000, 500);
        tracker.RecordUsage(Constants.Models.Gpt5, "session2", 2000, 1000);

        // Assert
        var session1Usage = tracker.GetSessionUsage("session1");
        var session2Usage = tracker.GetSessionUsage("session2");
        Assert.Equal(1000, session1Usage.TotalInputTokens);
        Assert.Equal(2000, session2Usage.TotalInputTokens);
        Assert.NotEqual(session1Usage.EstimatedCost, session2Usage.EstimatedCost);
    }

    [Fact]
    public void GetModelUsage_UnknownModel_ReturnsEmptyUsage()
    {
        // Arrange
        var tracker = new CostTracker();

        // Act
        var usage = tracker.GetModelUsage("unknown-model");

        // Assert
        Assert.Equal("unknown-model", usage.Model);
        Assert.Equal(0, usage.TotalInputTokens);
        Assert.Equal(0, usage.TotalOutputTokens);
        Assert.Equal(0, usage.RequestCount);
    }

    [Fact]
    public void GetSessionUsage_UnknownSession_ReturnsEmptyUsage()
    {
        // Arrange
        var tracker = new CostTracker();

        // Act
        var usage = tracker.GetSessionUsage("unknown-session");

        // Assert
        Assert.Equal("unknown-session", usage.SessionId);
        Assert.Equal(0, usage.TotalInputTokens);
        Assert.Equal(0, usage.TotalOutputTokens);
    }
}
