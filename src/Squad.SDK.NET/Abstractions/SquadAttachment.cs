namespace Squad.SDK.NET.Abstractions;

public sealed record SquadAttachment
{
    public string? Path { get; init; }
    public string? DisplayName { get; init; }
    /// <summary>Base64-encoded file data.</summary>
    public string? Data { get; init; }
    public string? MimeType { get; init; }
}
