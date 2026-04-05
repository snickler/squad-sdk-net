using System.Collections.Concurrent;

namespace Squad.SDK.NET.Runtime;

/// <summary>
/// Tracks token usage and estimated costs per model and session.
/// </summary>
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

    /// <summary>
    /// Records token usage for the specified model and session.
    /// </summary>
    /// <param name="model">The model identifier (e.g., <see cref="Constants.Models.Gpt5"/>).</param>
    /// <param name="sessionId">The session that consumed the tokens.</param>
    /// <param name="inputTokens">Number of input (prompt) tokens used.</param>
    /// <param name="outputTokens">Number of output (completion) tokens used.</param>
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

    /// <summary>
    /// Estimates the cost in USD for the given token counts and model.
    /// </summary>
    /// <param name="model">The model identifier.</param>
    /// <param name="inputTokens">Number of input tokens.</param>
    /// <param name="outputTokens">Number of output tokens.</param>
    /// <returns>Estimated cost in USD, or <c>0</c> if the model is unknown.</returns>
    public decimal EstimateCost(string model, int inputTokens, int outputTokens)
    {
        if (!ModelPricing.TryGetValue(model, out var pricing))
            return 0m;

        return pricing.Input * inputTokens / 1_000_000m
             + pricing.Output * outputTokens / 1_000_000m;
    }

    /// <summary>Returns aggregated usage for the specified model.</summary>
    /// <param name="model">The model identifier.</param>
    /// <returns>A <see cref="ModelUsage"/> snapshot; zeroed if no usage has been recorded.</returns>
    public ModelUsage GetModelUsage(string model) =>
        _usageByModel.TryGetValue(model, out var usage)
            ? usage
            : new ModelUsage { Model = model };

    /// <summary>Returns aggregated usage for the specified session.</summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>A <see cref="SessionUsage"/> snapshot; zeroed if no usage has been recorded.</returns>
    public SessionUsage GetSessionUsage(string sessionId) =>
        _usageBySession.TryGetValue(sessionId, out var usage)
            ? usage
            : new SessionUsage { SessionId = sessionId };

    /// <summary>Returns a summary of all tracked usage broken down by model and session.</summary>
    /// <returns>A <see cref="CostSummary"/> containing totals and per-model/session breakdowns.</returns>
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

/// <summary>
/// Aggregated token usage and cost for a single model.
/// </summary>
public sealed record ModelUsage
{
    /// <summary>Gets the model identifier.</summary>
    public required string Model { get; init; }
    /// <summary>Gets the total number of input tokens consumed.</summary>
    public int TotalInputTokens { get; init; }
    /// <summary>Gets the total number of output tokens produced.</summary>
    public int TotalOutputTokens { get; init; }
    /// <summary>Gets the estimated cost in USD.</summary>
    public decimal EstimatedCost { get; init; }
    /// <summary>Gets the number of requests made to this model.</summary>
    public int RequestCount { get; init; }
}

/// <summary>
/// Aggregated token usage and cost for a single session.
/// </summary>
public sealed record SessionUsage
{
    /// <summary>Gets the session identifier.</summary>
    public required string SessionId { get; init; }
    /// <summary>Gets the total number of input tokens consumed.</summary>
    public int TotalInputTokens { get; init; }
    /// <summary>Gets the total number of output tokens produced.</summary>
    public int TotalOutputTokens { get; init; }
    /// <summary>Gets the estimated cost in USD.</summary>
    public decimal EstimatedCost { get; init; }
}

/// <summary>
/// Overall cost summary with per-model and per-session breakdowns.
/// </summary>
public sealed record CostSummary
{
    /// <summary>Gets the total number of input tokens across all models.</summary>
    public int TotalInputTokens { get; init; }
    /// <summary>Gets the total number of output tokens across all models.</summary>
    public int TotalOutputTokens { get; init; }
    /// <summary>Gets the total estimated cost in USD across all models.</summary>
    public decimal TotalEstimatedCost { get; init; }
    /// <summary>Gets usage broken down by model identifier.</summary>
    public IReadOnlyDictionary<string, ModelUsage> ByModel { get; init; } = new Dictionary<string, ModelUsage>();
    /// <summary>Gets usage broken down by session identifier.</summary>
    public IReadOnlyDictionary<string, SessionUsage> BySession { get; init; } = new Dictionary<string, SessionUsage>();
}
