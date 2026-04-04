namespace Squad.SDK.NET.Abstractions;

/// <summary>
/// Options for sending a message to an <see cref="ISquadSession"/>.
/// </summary>
public sealed record SquadMessageOptions
{
    /// <summary>The user prompt text to send.</summary>
    public required string Prompt { get; init; }

    /// <summary>Optional file attachments to include with the message.</summary>
    public IReadOnlyList<SquadAttachment>? Attachments { get; init; }
}
