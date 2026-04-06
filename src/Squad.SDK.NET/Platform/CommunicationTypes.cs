namespace Squad.SDK.NET.Platform;

/// <summary>
/// Identifies the communication channel used for agent-human communication.
/// </summary>
public enum CommunicationChannel
{
    /// <summary>GitHub Discussions — posts agent updates as discussion threads.</summary>
    GitHubDiscussions,
    /// <summary>Azure DevOps work item discussions.</summary>
    AdoWorkItems,
    /// <summary>Microsoft Teams via Microsoft Graph API.</summary>
    TeamsGraph,
    /// <summary>File-based logging (always available, no external service required).</summary>
    FileLog
}

/// <summary>
/// A reply received from a human on a communication channel.
/// </summary>
public sealed record CommunicationReply
{
    /// <summary>Gets the author of the reply.</summary>
    public required string Author { get; init; }
    /// <summary>Gets the message body.</summary>
    public required string Body { get; init; }
    /// <summary>Gets the timestamp when the reply was posted.</summary>
    public required DateTimeOffset Timestamp { get; init; }
    /// <summary>Gets the platform-specific identifier for the reply.</summary>
    public required string Id { get; init; }
}

/// <summary>
/// Configuration for a communication channel.
/// </summary>
public sealed record CommunicationConfig
{
    /// <summary>Gets the communication channel to use.</summary>
    public required CommunicationChannel Channel { get; init; }
    /// <summary>Gets a value indicating whether to post session summaries after agent work.</summary>
    public bool PostAfterSession { get; init; }
    /// <summary>Gets a value indicating whether to post decisions that need human review.</summary>
    public bool PostDecisions { get; init; }
    /// <summary>Gets a value indicating whether to post escalations when agents are blocked.</summary>
    public bool PostEscalations { get; init; }
    /// <summary>Gets optional adapter-specific configuration, keyed by channel name.</summary>
    public IReadOnlyDictionary<string, object>? AdapterConfig { get; init; }
}

/// <summary>
/// Options for posting an update to a communication channel.
/// </summary>
public sealed record PostUpdateOptions
{
    /// <summary>Gets the update title.</summary>
    public required string Title { get; init; }
    /// <summary>Gets the update body text.</summary>
    public required string Body { get; init; }
    /// <summary>Gets an optional category (e.g. discussion category name).</summary>
    public string? Category { get; init; }
    /// <summary>Gets the agent or role posting the update.</summary>
    public string? Author { get; init; }
}

/// <summary>
/// Result of posting an update to a communication channel.
/// </summary>
public sealed record PostUpdateResult
{
    /// <summary>Gets the platform-specific identifier for the posted thread/item.</summary>
    public required string Id { get; init; }
    /// <summary>Gets the optional URL to the posted item.</summary>
    public string? Url { get; init; }
}

/// <summary>
/// Options for polling a channel for human replies.
/// </summary>
public sealed record PollForRepliesOptions
{
    /// <summary>Gets the thread or discussion ID to check for replies.</summary>
    public required string ThreadId { get; init; }
    /// <summary>Gets the timestamp; only replies newer than this are returned.</summary>
    public required DateTimeOffset Since { get; init; }
}

/// <summary>
/// Pluggable adapter for agent-human communication.
/// Abstracts the communication channel so Squad can post updates and read replies
/// from GitHub Discussions, ADO Work Item discussions, Teams, or plain log files.
/// </summary>
public interface ICommunicationAdapter
{
    /// <summary>Gets the communication channel this adapter handles.</summary>
    CommunicationChannel Channel { get; }

    /// <summary>
    /// Posts an update to the communication channel.
    /// Used by Scribe (session summaries), Ralph (board status), and agents (escalations).
    /// </summary>
    /// <param name="options">Update options (title, body, category, author).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing the posted item's ID and optional URL.</returns>
    Task<PostUpdateResult> PostUpdateAsync(PostUpdateOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Polls for human replies since the specified timestamp.
    /// </summary>
    /// <param name="options">Poll options containing thread ID and since timestamp.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New replies from humans on the channel.</returns>
    Task<IReadOnlyList<CommunicationReply>> PollForRepliesAsync(PollForRepliesOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a URL that humans can open on any device (phone, browser, desktop).
    /// Returns <see langword="null"/> if the channel has no web UI (e.g., file-log).
    /// </summary>
    /// <param name="threadId">The thread or item ID.</param>
    /// <returns>A URL string, or <see langword="null"/> if not applicable.</returns>
    string? GetNotificationUrl(string threadId);
}
