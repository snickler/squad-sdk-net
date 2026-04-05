namespace Squad.SDK.NET.Config;

/// <summary>Configuration for telemetry collection and export.</summary>
public sealed record TelemetryConfig
{
    /// <summary>Gets a value indicating whether telemetry collection is enabled.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Gets the OTLP endpoint to which telemetry data is exported.</summary>
    public string? Endpoint { get; init; }

    /// <summary>Gets the logical service name used in telemetry data.</summary>
    public string? ServiceName { get; init; }

    /// <summary>Gets the sampling rate for traces, where <c>1.0</c> means all traces are captured.</summary>
    public double SampleRate { get; init; } = 1.0;

    /// <summary>Gets a value indicating whether .NET Aspire telemetry defaults should be applied.</summary>
    public bool AspireDefaults { get; init; }
}
