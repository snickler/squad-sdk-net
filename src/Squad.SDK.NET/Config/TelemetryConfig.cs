namespace Squad.SDK.NET.Config;

public sealed record TelemetryConfig
{
    public bool Enabled { get; init; } = true;
    public string? Endpoint { get; init; }
    public string? ServiceName { get; init; }
    public double SampleRate { get; init; } = 1.0;
    public bool AspireDefaults { get; init; }
}
