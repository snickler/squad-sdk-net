namespace Squad.SDK.NET.Config;

public sealed record CastingConfig
{
    public IReadOnlyList<string> AllowlistUniverses { get; init; } = [];
    public OverflowStrategy OverflowStrategy { get; init; } = OverflowStrategy.Rotate;
    public int? Capacity { get; init; }
}

public enum OverflowStrategy { Rotate, Queue, Reject }
