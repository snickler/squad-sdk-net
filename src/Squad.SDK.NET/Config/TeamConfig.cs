namespace Squad.SDK.NET.Config;

public sealed record TeamConfig
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? DefaultModel { get; init; }
    public ModelTier DefaultTier { get; init; } = ModelTier.Standard;
}
