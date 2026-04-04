namespace Squad.SDK.NET.Platform;

public enum PlatformType
{
    GitHub,
    AzureDevOps,
    Local,
    Unknown
}

public interface IPlatformAdapter
{
    PlatformType Type { get; }
    Task<IReadOnlyList<WorkItem>> GetWorkItemsAsync(string? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PullRequestInfo>> GetPullRequestsAsync(string? state = null, CancellationToken cancellationToken = default);
    Task<WorkItem> CreateWorkItemAsync(string title, string? description = null, CancellationToken cancellationToken = default);
}

public sealed record WorkItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? State { get; init; }
    public string? AssignedTo { get; init; }
    public IReadOnlyList<string> Labels { get; init; } = [];
    public string? Url { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed record PullRequestInfo
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? State { get; init; }
    public string? Author { get; init; }
    public string? SourceBranch { get; init; }
    public string? TargetBranch { get; init; }
    public string? Url { get; init; }
    public IReadOnlyList<string> Labels { get; init; } = [];
    public DateTimeOffset? CreatedAt { get; init; }
}
