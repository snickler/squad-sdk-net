namespace Squad.SDK.NET.Platform;

/// <summary>Identifies the communication channel used by an adapter.</summary>
public enum CommunicationChannel
{
    /// <summary>GitHub Discussions — posts updates as discussion threads.</summary>
    GitHubDiscussions,
    /// <summary>Azure DevOps work-item discussions.</summary>
    AdoWorkItems,
    /// <summary>Microsoft Teams via Microsoft Graph API.</summary>
    TeamsGraph,
    /// <summary>Plain file-based log (always available, no external service required).</summary>
    FileLog
}

/// <summary>
/// A reply received from a human on a communication channel.
/// </summary>
public sealed record CommunicationReply
{
    /// <summary>Gets the display name or identifier of the reply author.</summary>
    public required string Author { get; init; }
    /// <summary>Gets the body text of the reply.</summary>
    public required string Body { get; init; }
    /// <summary>Gets the timestamp when the reply was posted.</summary>
    public required DateTimeOffset Timestamp { get; init; }
    /// <summary>Gets the platform-specific identifier for this reply.</summary>
    public required string Id { get; init; }
}

/// <summary>
/// Configuration for a communication channel.
/// </summary>
public sealed record CommunicationConfig
{
    /// <summary>Gets the channel to use.</summary>
    public required CommunicationChannel Channel { get; init; }
    /// <summary>Gets a value indicating whether session summaries are posted after agent work.</summary>
    public bool PostAfterSession { get; init; }
    /// <summary>Gets a value indicating whether decisions requiring human review are posted.</summary>
    public bool PostDecisions { get; init; }
    /// <summary>Gets a value indicating whether escalations when agents are blocked are posted.</summary>
    public bool PostEscalations { get; init; }
    /// <summary>Gets optional adapter-specific configuration, keyed by channel name.</summary>
    public IReadOnlyDictionary<string, object>? AdapterConfig { get; init; }
}

/// <summary>
/// Options for posting an update to a communication channel.
/// </summary>
public sealed record PostUpdateOptions
{
    /// <summary>Gets the title of the update.</summary>
    public required string Title { get; init; }
    /// <summary>Gets the body of the update.</summary>
    public required string Body { get; init; }
    /// <summary>Gets the optional category for the update (e.g., discussion category on GitHub).</summary>
    public string? Category { get; init; }
    /// <summary>Gets the optional agent or role posting the update.</summary>
    public string? Author { get; init; }
}

/// <summary>
/// Result of posting an update to a communication channel.
/// </summary>
public sealed record PostUpdateResult
{
    /// <summary>Gets the platform-specific thread or item identifier.</summary>
    public required string Id { get; init; }
    /// <summary>Gets the optional URL where the update can be viewed.</summary>
    public string? Url { get; init; }
}

/// <summary>
/// Pluggable communication adapter interface — abstracts agent-human communication
/// across GitHub Discussions, ADO work-item discussions, Teams, and file logs.
/// </summary>
public interface ICommunicationAdapter
{
    /// <summary>Gets the channel this adapter communicates through.</summary>
    CommunicationChannel Channel { get; }

    /// <summary>
    /// Posts an update to the communication channel.
    /// Used by the Scribe role (session summaries), Ralph (board status), and agents (escalations).
    /// </summary>
    /// <param name="options">Options describing the update to post.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing the thread or item identifier and optional URL.</returns>
    Task<PostUpdateResult> PostUpdateAsync(PostUpdateOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Polls for replies received since <paramref name="since"/> on the given thread.
    /// </summary>
    /// <param name="threadId">The thread or discussion identifier to poll.</param>
    /// <param name="since">Only return replies posted after this timestamp.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of new replies from humans on the channel.</returns>
    Task<IReadOnlyList<CommunicationReply>> PollForRepliesAsync(
        string threadId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a URL that humans can open on any device to view the thread.
    /// Returns <see langword="null"/> if the channel has no web UI (e.g., file-log).
    /// </summary>
    /// <param name="threadId">The thread identifier.</param>
    /// <returns>A URL string, or <see langword="null"/> if not applicable.</returns>
    string? GetNotificationUrl(string threadId);

    /// <summary>
    /// Clears any locally cached credentials for this adapter.
    /// Implementations that do not persist credentials may use the default no-op behavior.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the logout operation.</returns>
    Task LogoutAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
