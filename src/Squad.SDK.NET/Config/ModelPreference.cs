namespace Squad.SDK.NET.Config;

public sealed record ModelPreference
{
    public required string Preferred { get; init; }
    public string? Rationale { get; init; }
    public string? Fallback { get; init; }
}
