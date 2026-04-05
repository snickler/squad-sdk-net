namespace Squad.SDK.NET.Config;

/// <summary>Specifies a preferred model and an optional fallback for an agent or team.</summary>
public sealed record ModelPreference
{
    /// <summary>Gets the identifier of the preferred model.</summary>
    public required string Preferred { get; init; }

    /// <summary>Gets the rationale for choosing this model preference.</summary>
    public string? Rationale { get; init; }

    /// <summary>Gets the identifier of the fallback model used when the preferred model is unavailable.</summary>
    public string? Fallback { get; init; }
}
