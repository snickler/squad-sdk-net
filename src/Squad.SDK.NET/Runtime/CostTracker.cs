using System.Collections.Concurrent;

namespace Squad.SDK.NET.Runtime;

public sealed class CostTracker
{
    private readonly ConcurrentDictionary<string, ModelUsage> _usageByModel = new();
    private readonly ConcurrentDictionary<string, SessionUsage> _usageBySession = new();

    private static readonly Dictionary<string, (decimal Input, decimal Output)> ModelPricing = new()
    {
        [Constants.Models.Gpt5]        = (2.50m,  10.00m),
        [Constants.Models.Gpt5Mini]    = (0.40m,   1.60m),
        [Constants.Models.ClaudeOpus]  = (15.00m, 75.00m),
        [Constants.Models.ClaudeSonnet]= (3.00m,  15.00m),
        [Constants.Models.ClaudeHaiku] = (0.80m,   4.00m),
    };

    public void RecordUsage(string model, string sessionId, int inputTokens, int outputTokens)
    {
        var cost = EstimateCost(model, inputTokens, outputTokens);

        _usageByModel.AddOrUpdate(
            model,
            _ => new ModelUsage
            {
                Model = model,
                TotalInputTokens = inputTokens,
                TotalOutputTokens = outputTokens,
                EstimatedCost = cost,
                RequestCount = 1
            },
            (_, existing) => existing with
            {
                TotalInputTokens = existing.TotalInputTokens + inputTokens,
                TotalOutputTokens = existing.TotalOutputTokens + outputTokens,
                EstimatedCost = existing.EstimatedCost + cost,
                RequestCount = existing.RequestCount + 1
            });

        _usageBySession.AddOrUpdate(
            sessionId,
            _ => new SessionUsage
            {
                SessionId = sessionId,
                TotalInputTokens = inputTokens,
                TotalOutputTokens = outputTokens,
                EstimatedCost = cost
            },
            (_, existing) => existing with
            {
                TotalInputTokens = existing.TotalInputTokens + inputTokens,
                TotalOutputTokens = existing.TotalOutputTokens + outputTokens,
                EstimatedCost = existing.EstimatedCost + cost
            });
    }

    public decimal EstimateCost(string model, int inputTokens, int outputTokens)
    {
        if (!ModelPricing.TryGetValue(model, out var pricing))
            return 0m;

        return pricing.Input * inputTokens / 1_000_000m
             + pricing.Output * outputTokens / 1_000_000m;
    }

    public ModelUsage GetModelUsage(string model) =>
        _usageByModel.TryGetValue(model, out var usage)
            ? usage
            : new ModelUsage { Model = model };

    public SessionUsage GetSessionUsage(string sessionId) =>
        _usageBySession.TryGetValue(sessionId, out var usage)
            ? usage
            : new SessionUsage { SessionId = sessionId };

    public CostSummary GetTotalSummary()
    {
        var byModel = _usageByModel.ToDictionary(kv => kv.Key, kv => kv.Value);
        var bySession = _usageBySession.ToDictionary(kv => kv.Key, kv => kv.Value);

        return new CostSummary
        {
            TotalInputTokens = byModel.Values.Sum(m => m.TotalInputTokens),
            TotalOutputTokens = byModel.Values.Sum(m => m.TotalOutputTokens),
            TotalEstimatedCost = byModel.Values.Sum(m => m.EstimatedCost),
            ByModel = byModel,
            BySession = bySession
        };
    }

    /// <summary>
    /// Clears all tracked token usage across models and sessions.
    /// </summary>
    public void Reset()
    {
        _usageByModel.Clear();
        _usageBySession.Clear();
    }
}

public sealed record ModelUsage
{
    public required string Model { get; init; }
    public int TotalInputTokens { get; init; }
    public int TotalOutputTokens { get; init; }
    public decimal EstimatedCost { get; init; }
    public int RequestCount { get; init; }
}

public sealed record SessionUsage
{
    public required string SessionId { get; init; }
    public int TotalInputTokens { get; init; }
    public int TotalOutputTokens { get; init; }
    public decimal EstimatedCost { get; init; }
}

public sealed record CostSummary
{
    public int TotalInputTokens { get; init; }
    public int TotalOutputTokens { get; init; }
    public decimal TotalEstimatedCost { get; init; }
    public IReadOnlyDictionary<string, ModelUsage> ByModel { get; init; } = new Dictionary<string, ModelUsage>();
    public IReadOnlyDictionary<string, SessionUsage> BySession { get; init; } = new Dictionary<string, SessionUsage>();
}
