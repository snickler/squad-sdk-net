namespace Squad.SDK.NET.Sharing;

public sealed record ExportedSquad
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    public required string ConfigJson { get; init; }
    public IReadOnlyList<ExportedAgent> Agents { get; init; } = [];
    public DateTimeOffset ExportedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record ExportedAgent
{
    public required string Name { get; init; }
    public required string Role { get; init; }
    public string? Charter { get; init; }
    public string? Prompt { get; init; }
}

public sealed record ImportResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public string? ImportedPath { get; init; }
}
