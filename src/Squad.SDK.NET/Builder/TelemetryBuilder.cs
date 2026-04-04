using Squad.SDK.NET.Config;

namespace Squad.SDK.NET.Builder;

public sealed class TelemetryBuilder
{
    private bool _enabled = true;
    private string? _endpoint;
    private string? _serviceName;
    private double _sampleRate = 1.0;
    private bool _aspireDefaults;

    public TelemetryBuilder Enabled(bool enabled) { _enabled = enabled; return this; }
    public TelemetryBuilder Endpoint(string endpoint) { _endpoint = endpoint; return this; }
    public TelemetryBuilder ServiceName(string serviceName) { _serviceName = serviceName; return this; }
    public TelemetryBuilder SampleRate(double rate) { _sampleRate = rate; return this; }
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
