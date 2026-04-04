namespace Squad.SDK.NET.Marketplace;

public sealed record MarketplaceManifest
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    public string? License { get; init; }
    public string? Homepage { get; init; }
    public string? Repository { get; init; }
    public ManifestCategory Category { get; init; } = ManifestCategory.General;
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<ManifestCapability> Capabilities { get; init; } = [];
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

public enum ManifestCategory
{
    General,
    Development,
    Testing,
    Documentation,
    Security,
    DevOps,
    Data,
    AI
}

public sealed record ManifestCapability
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool Required { get; init; }
}
