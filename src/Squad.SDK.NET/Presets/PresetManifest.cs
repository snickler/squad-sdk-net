namespace Squad.SDK.NET.Presets;

/// <summary>
/// Agent definition within a preset manifest.
/// </summary>
public sealed record PresetAgent
{
    /// <summary>Gets the agent name (used as the directory name).</summary>
    public required string Name { get; init; }

    /// <summary>Gets the agent role (e.g. <c>lead</c>, <c>reviewer</c>, <c>devrel</c>).</summary>
    public required string Role { get; init; }

    /// <summary>Gets the optional short description of this agent's purpose.</summary>
    public string? Description { get; init; }
}

/// <summary>
/// Preset manifest — describes a preset and its included agents.
/// Stored as <c>preset.json</c> in the preset directory.
/// </summary>
public sealed record PresetManifest
{
    /// <summary>Gets the preset display name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the version identifier.</summary>
    public required string Version { get; init; }

    /// <summary>Gets the short description of what this preset is for.</summary>
    public required string Description { get; init; }

    /// <summary>Gets the agents included in this preset.</summary>
    public required IReadOnlyList<PresetAgent> Agents { get; init; }

    /// <summary>Gets the optional author attribution.</summary>
    public string? Author { get; init; }

    /// <summary>Gets the optional tags for discovery.</summary>
    public IReadOnlyList<string>? Tags { get; init; }
}

/// <summary>
/// Result of applying a single agent from a preset.
/// </summary>
public sealed record PresetApplyResult
{
    /// <summary>Gets the agent name.</summary>
    public required string Agent { get; init; }

    /// <summary>Gets the apply status.</summary>
    public required PresetApplyStatus Status { get; init; }

    /// <summary>Gets the optional reason string (for <see cref="PresetApplyStatus.Skipped"/> or <see cref="PresetApplyStatus.Error"/>).</summary>
    public string? Reason { get; init; }
}

/// <summary>Status returned when applying a preset agent.</summary>
public enum PresetApplyStatus
{
    /// <summary>The agent was successfully installed.</summary>
    Installed,

    /// <summary>The agent was skipped (already exists and force was not set).</summary>
    Skipped,

    /// <summary>An error occurred while installing the agent.</summary>
    Error
}
