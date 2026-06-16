namespace Squad.SDK.NET.Config;

/// <summary>Options that bridge upstream Squad.Agents.AI-style settings into the current SDK shape.</summary>
public sealed record SquadAgentsAiOptions
{
    /// <summary>Gets or sets the optional squad folder path.</summary>
    public string? SquadFolderPath { get; init; }

    /// <summary>Gets or sets the optional CLI path.</summary>
    public string? CliPath { get; init; }

    /// <summary>Gets the CLI arguments to pass through.</summary>
    public IReadOnlyList<string> CliArgs { get; init; } = [];

    /// <summary>Gets or sets the optional working directory.</summary>
    public string? Cwd { get; init; }

    /// <summary>Gets the environment variables to pass through.</summary>
    public IReadOnlyDictionary<string, string?> Environment { get; init; } = new Dictionary<string, string?>();

    /// <summary>Gets or sets an explicit GitHub token.</summary>
    public string? GitHubToken { get; init; }

    /// <summary>Gets or sets an async GitHub token provider.</summary>
    public Func<CancellationToken, ValueTask<string?>>? GitHubTokenProvider { get; init; }

    /// <summary>Gets or sets whether trace events should be emitted.</summary>
    public bool TraceEvents { get; init; }

    /// <summary>Gets or sets the agent name.</summary>
    public string AgentName { get; init; } = "Squad";

    /// <summary>Gets or sets the agent file name.</summary>
    public string? AgentFileName { get; init; } = "squad";

    /// <summary>Gets or sets the agent instructions.</summary>
    public string? Instructions { get; init; }
}
