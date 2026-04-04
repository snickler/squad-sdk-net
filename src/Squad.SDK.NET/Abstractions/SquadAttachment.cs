namespace Squad.SDK.NET.Abstractions;

/// <summary>
/// Represents a file attachment included with a message sent to an agent session.
/// </summary>
public sealed record SquadAttachment
{
    /// <summary>The file path of the attachment.</summary>
    public string? Path { get; init; }

    /// <summary>A human-friendly display name for the attachment.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Base64-encoded file data.</summary>
    public string? Data { get; init; }

    /// <summary>The MIME type of the attachment (e.g., <c>"image/png"</c>).</summary>
    public string? MimeType { get; init; }
}
