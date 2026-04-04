namespace Squad.SDK.NET.Platform;

/// <summary>Identifies the hosting platform for a repository.</summary>
public enum PlatformType
{
    /// <summary>GitHub-hosted repository.</summary>
    GitHub,
    /// <summary>Azure DevOps-hosted repository.</summary>
    AzureDevOps,
    /// <summary>Local git repository without a recognized remote.</summary>
    Local,
    /// <summary>Platform could not be determined.</summary>
    Unknown
}

/// <summary>
/// Adapter interface for platform-specific operations such as work items and pull requests.
/// </summary>
public interface IPlatformAdapter
{
    /// <summary>Gets the platform type this adapter supports.</summary>
    PlatformType Type { get; }

    /// <summary>Retrieves work items, optionally filtered by a query.</summary>
    /// <param name="query">Optional query filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of matching <see cref="WorkItem"/> instances.</returns>
    Task<IReadOnlyList<WorkItem>> GetWorkItemsAsync(string? query = null, CancellationToken cancellationToken = default);

    /// <summary>Retrieves pull requests, optionally filtered by state.</summary>
    /// <param name="state">Optional state filter (e.g., "open", "closed").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of matching <see cref="PullRequestInfo"/> instances.</returns>
    Task<IReadOnlyList<PullRequestInfo>> GetPullRequestsAsync(string? state = null, CancellationToken cancellationToken = default);

    /// <summary>Creates a new work item on the platform.</summary>
    /// <param name="title">Work item title.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created <see cref="WorkItem"/>.</returns>
    Task<WorkItem> CreateWorkItemAsync(string title, string? description = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a work item from a platform (e.g., GitHub Issue, Azure DevOps Work Item).
/// </summary>
public sealed record WorkItem
{
    /// <summary>Gets the platform-specific work item identifier.</summary>
    public required string Id { get; init; }
    /// <summary>Gets the work item title.</summary>
    public required string Title { get; init; }
    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }
    /// <summary>Gets the current state (e.g., "open", "closed").</summary>
    public string? State { get; init; }
    /// <summary>Gets the assignee, if any.</summary>
    public string? AssignedTo { get; init; }
    /// <summary>Gets the labels applied to this work item.</summary>
    public IReadOnlyList<string> Labels { get; init; } = [];
    /// <summary>Gets the URL to the work item on the platform.</summary>
    public string? Url { get; init; }
    /// <summary>Gets the creation timestamp.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
    /// <summary>Gets the last-updated timestamp.</summary>
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Represents a pull request from a platform.
/// </summary>
public sealed record PullRequestInfo
{
    /// <summary>Gets the platform-specific pull request identifier.</summary>
    public required string Id { get; init; }
    /// <summary>Gets the pull request title.</summary>
    public required string Title { get; init; }
    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }
    /// <summary>Gets the current state (e.g., "open", "merged").</summary>
    public string? State { get; init; }
    /// <summary>Gets the author of the pull request.</summary>
    public string? Author { get; init; }
    /// <summary>Gets the source branch name.</summary>
    public string? SourceBranch { get; init; }
    /// <summary>Gets the target branch name.</summary>
    public string? TargetBranch { get; init; }
    /// <summary>Gets the URL to the pull request on the platform.</summary>
    public string? Url { get; init; }
    /// <summary>Gets the labels applied to this pull request.</summary>
    public IReadOnlyList<string> Labels { get; init; } = [];
    /// <summary>Gets the creation timestamp.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
}
