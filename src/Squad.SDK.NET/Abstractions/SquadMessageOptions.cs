namespace Squad.SDK.NET.Abstractions;

public sealed record SquadMessageOptions
{
    public required string Prompt { get; init; }
    public IReadOnlyList<SquadAttachment>? Attachments { get; init; }
}
