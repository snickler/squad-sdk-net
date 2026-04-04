namespace Squad.SDK.NET.Roles;

/// <summary>
/// Defines a role that can be assigned to an agent, including default model, tools, and prompt template.
/// </summary>
public sealed record BaseRole
{
    /// <summary>Gets the unique role identifier.</summary>
    public required string Id { get; init; }
    /// <summary>Gets the human-readable role name.</summary>
    public required string Name { get; init; }
    /// <summary>Gets an optional role description.</summary>
    public string? Description { get; init; }
    /// <summary>Gets the role category.</summary>
    public RoleCategory Category { get; init; } = RoleCategory.Engineering;
    /// <summary>Gets the optional default model for this role.</summary>
    public string? DefaultModel { get; init; }
    /// <summary>Gets the default tools available to this role.</summary>
    public IReadOnlyList<string> DefaultTools { get; init; } = [];
    /// <summary>Gets the areas of expertise for this role.</summary>
    public IReadOnlyList<string> Expertise { get; init; } = [];
    /// <summary>Gets the optional prompt template for agents using this role.</summary>
    public string? PromptTemplate { get; init; }
}

/// <summary>Categorizes roles by functional area.</summary>
public enum RoleCategory
{
    /// <summary>Software engineering roles.</summary>
    Engineering,
    /// <summary>Quality assurance and testing roles.</summary>
    Testing,
    /// <summary>Documentation and technical writing roles.</summary>
    Documentation,
    /// <summary>System architecture and design roles.</summary>
    Architecture,
    /// <summary>Developer relations and advocacy roles.</summary>
    DevRel,
    /// <summary>User experience and interface design roles.</summary>
    Design,
    /// <summary>Security engineering roles.</summary>
    Security,
    /// <summary>Operations and DevOps roles.</summary>
    Ops
}
