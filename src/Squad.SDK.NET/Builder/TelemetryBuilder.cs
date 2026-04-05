using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Builder;

/// <summary>
/// Fluent builder for configuring a <see cref="TelemetryConfig"/>.
/// </summary>
/// <seealso cref="SquadBuilder"/>
public sealed class TelemetryBuilder
{
    private bool _enabled = true;
    private string? _endpoint;
    private string? _serviceName;
    private double _sampleRate = 1.0;
    private bool _aspireDefaults;

    /// <summary>Enables or disables telemetry collection.</summary>
    /// <param name="enabled"><see langword="true"/> to enable telemetry.</param>
    /// <returns>This builder instance for chaining.</returns>
    public TelemetryBuilder Enabled(bool enabled) { _enabled = enabled; return this; }

    /// <summary>Sets the OTLP exporter endpoint.</summary>
    /// <param name="endpoint">The telemetry endpoint URI.</param>
    /// <returns>This builder instance for chaining.</returns>
    public TelemetryBuilder Endpoint(string endpoint) { _endpoint = endpoint; return this; }

    /// <summary>Sets the service name reported in telemetry.</summary>
    /// <param name="serviceName">The service name.</param>
    /// <returns>This builder instance for chaining.</returns>
    public TelemetryBuilder ServiceName(string serviceName) { _serviceName = serviceName; return this; }

    /// <summary>Sets the sampling rate for telemetry data.</summary>
    /// <param name="rate">A value between 0.0 and 1.0 indicating the sampling probability.</param>
    /// <returns>This builder instance for chaining.</returns>
    public TelemetryBuilder SampleRate(double rate) { _sampleRate = rate; return this; }

    /// <summary>Enables or disables .NET Aspire default telemetry configuration.</summary>
    /// <param name="enabled"><see langword="true"/> to apply Aspire defaults.</param>
    /// <returns>This builder instance for chaining.</returns>
    public TelemetryBuilder AspireDefaults(bool enabled = true) { _aspireDefaults = enabled; return this; }

    internal TelemetryConfig Build() => new()
    {
        Enabled = _enabled,
        Endpoint = _endpoint,
        ServiceName = _serviceName,
        SampleRate = _sampleRate,
        AspireDefaults = _aspireDefaults
    };
}
