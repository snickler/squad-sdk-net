namespace Squad.SDK.NET.Resolution;

public sealed record ResolvedSquadPaths
{
    public required SquadMode Mode { get; init; }
    public required string ProjectDir { get; init; }
    public string? TeamDir { get; init; }
    public string? PersonalDir { get; init; }
    public string? Name { get; init; }
    public bool IsLegacy { get; init; }
}

public enum SquadMode { Project, Personal, Global }

public sealed record SquadDirConfig
{
    public string Version { get; init; } = "1.0";
    public string? TeamRoot { get; init; }
    public string? ProjectKey { get; init; }
    public bool ExtractionDisabled { get; init; }
}
