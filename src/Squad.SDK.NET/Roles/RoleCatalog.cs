using Squad.SDK.NET.Agents;

namespace Squad.SDK.NET.Roles;

/// <summary>
/// Static catalog of predefined <see cref="BaseRole"/> definitions for common engineering roles.
/// </summary>
public static class RoleCatalog
{
    private static readonly Dictionary<string, BaseRole> s_roles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["lead"] = new BaseRole
        {
            Id = "lead",
            Name = "Team Lead",
            Description = "Coordinates team efforts, makes architectural decisions, and ensures quality.",
            Category = RoleCategory.Engineering,
            DefaultModel = "claude-sonnet-4.6",
            Expertise = ["architecture", "code-review", "planning", "coordination"],
            PromptTemplate = "You are the team lead. Coordinate work across agents, make architectural decisions, and ensure high-quality output."
        },
        ["frontend"] = new BaseRole
        {
            Id = "frontend",
            Name = "Frontend Engineer",
            Description = "Builds user interfaces, implements designs, and ensures accessibility.",
            Category = RoleCategory.Engineering,
            Expertise = ["UI", "CSS", "accessibility", "responsive-design", "component-architecture"],
            PromptTemplate = "You are a frontend engineer. Focus on building clean, accessible, and performant user interfaces."
        },
        ["backend"] = new BaseRole
        {
            Id = "backend",
            Name = "Backend Engineer",
            Description = "Designs APIs, implements business logic, and manages data persistence.",
            Category = RoleCategory.Engineering,
            Expertise = ["APIs", "databases", "business-logic", "performance", "security"],
            PromptTemplate = "You are a backend engineer. Focus on building robust APIs, efficient data access, and secure business logic."
        },
        ["tester"] = new BaseRole
        {
            Id = "tester",
            Name = "Quality Engineer",
            Description = "Writes tests, identifies bugs, and ensures software reliability.",
            Category = RoleCategory.Testing,
            Expertise = ["unit-testing", "integration-testing", "test-automation", "bug-hunting"],
            PromptTemplate = "You are a quality engineer. Write comprehensive tests, identify edge cases, and ensure reliability."
        },
        ["scribe"] = new BaseRole
        {
            Id = "scribe",
            Name = "Technical Writer",
            Description = "Creates documentation, writes guides, and maintains README files.",
            Category = RoleCategory.Documentation,
            Expertise = ["technical-writing", "documentation", "tutorials", "API-docs"],
            PromptTemplate = "You are a technical writer. Create clear, comprehensive documentation and guides."
        },
        ["architect"] = new BaseRole
        {
            Id = "architect",
            Name = "Software Architect",
            Description = "Designs system architecture, evaluates trade-offs, and establishes patterns.",
            Category = RoleCategory.Architecture,
            Expertise = ["system-design", "patterns", "scalability", "trade-off-analysis"],
            PromptTemplate = "You are a software architect. Design systems with clean architecture, evaluate trade-offs, and establish best practices."
        },
        ["security"] = new BaseRole
        {
            Id = "security",
            Name = "Security Engineer",
            Description = "Reviews code for vulnerabilities, implements security controls, and audits systems.",
            Category = RoleCategory.Security,
            Expertise = ["vulnerability-assessment", "auth", "encryption", "compliance", "threat-modeling"],
            PromptTemplate = "You are a security engineer. Identify vulnerabilities, implement security controls, and ensure compliance."
        },
        ["devops"] = new BaseRole
        {
            Id = "devops",
            Name = "DevOps Engineer",
            Description = "Manages CI/CD pipelines, infrastructure, and deployment automation.",
            Category = RoleCategory.Ops,
            Expertise = ["CI/CD", "infrastructure", "containers", "monitoring", "automation"],
            PromptTemplate = "You are a DevOps engineer. Automate deployments, manage infrastructure, and ensure system reliability."
        },
        ["designer"] = new BaseRole
        {
            Id = "designer",
            Name = "UX Designer",
            Description = "Creates user experiences, designs interfaces, and conducts usability reviews.",
            Category = RoleCategory.Design,
            Expertise = ["UX", "UI-design", "accessibility", "user-research", "prototyping"],
            PromptTemplate = "You are a UX designer. Create intuitive user experiences and ensure accessibility."
        },
        ["devrel"] = new BaseRole
        {
            Id = "devrel",
            Name = "Developer Advocate",
            Description = "Creates developer content, builds demos, and engages with the community.",
            Category = RoleCategory.DevRel,
            Expertise = ["developer-experience", "demos", "content-creation", "community"],
            PromptTemplate = "You are a developer advocate. Create compelling demos, write developer content, and improve developer experience."
        },
        ["fact-checker"] = new BaseRole
        {
            Id = "fact-checker",
            Name = "Fact Checker",
            Description = "Verifies claims, detects hallucinations in LLM outputs, and cross-references team decisions for consistency.",
            Category = RoleCategory.Testing,
            Expertise = ["claim-verification", "hallucination-detection", "counter-hypothesis", "source-validation", "consistency-checks"],
            PromptTemplate = "You are a fact checker. Trust, but verify. For every claim, ask: \"What evidence supports this? What would disprove it?\" Verify URLs, package names, API endpoints, and version numbers actually exist. Run counter-hypotheses against key assumptions. Use confidence ratings (✅ Verified, ⚠️ Unverified, ❌ Contradicted). Flag issues clearly without being abrasive."
        }
    };

    /// <summary>Returns the role with the given identifier, or <see langword="null"/> if not found.</summary>
    /// <param name="roleId">The role identifier (case-insensitive).</param>
    /// <returns>The <see cref="BaseRole"/> if found; otherwise <see langword="null"/>.</returns>
    public static BaseRole? GetRole(string roleId)
    {
        s_roles.TryGetValue(roleId, out var role);
        return role;
    }

    /// <summary>Returns all predefined roles.</summary>
    /// <returns>A read-only list of all <see cref="BaseRole"/> definitions.</returns>
    public static IReadOnlyList<BaseRole> GetAllRoles() => s_roles.Values.ToList().AsReadOnly();

    /// <summary>Returns all roles matching the specified category.</summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>A read-only list of matching <see cref="BaseRole"/> definitions.</returns>
    public static IReadOnlyList<BaseRole> GetByCategory(RoleCategory category) =>
        s_roles.Values.Where(r => r.Category == category).ToList().AsReadOnly();

    /// <summary>Searches roles by name, description, and expertise keywords.</summary>
    /// <param name="query">Space-separated search terms.</param>
    /// <returns>A read-only list of matching <see cref="BaseRole"/> definitions.</returns>
    public static IReadOnlyList<BaseRole> SearchRoles(string query)
    {
        var terms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return s_roles.Values
            .Where(r => terms.Any(t =>
                r.Name.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                (r.Description?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
                r.Expertise.Any(e => e.Contains(t, StringComparison.OrdinalIgnoreCase))))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>Creates an <see cref="AgentCharter"/> from a predefined role.</summary>
    /// <param name="roleId">The role identifier to use.</param>
    /// <param name="agentName">The agent name.</param>
    /// <param name="additionalPrompt">Optional additional prompt text appended to the role's template.</param>
    /// <returns>An <see cref="AgentCharter"/> configured with the role's defaults.</returns>
    /// <exception cref="ArgumentException">Thrown when the role identifier is not found.</exception>
    public static AgentCharter UseRole(string roleId, string agentName, string? additionalPrompt = null)
    {
        var role = GetRole(roleId) ?? throw new ArgumentException($"Role '{roleId}' not found.", nameof(roleId));

        var prompt = role.PromptTemplate ?? $"You are a {role.Name}.";
        if (additionalPrompt is not null)
            prompt = $"{prompt}\n\n{additionalPrompt}";

        return new AgentCharter
        {
            Name = agentName,
            DisplayName = role.Name,
            Role = role.Id,
            Expertise = role.Expertise,
            Prompt = prompt,
            ModelPreference = role.DefaultModel,
            AllowedTools = role.DefaultTools.Count > 0 ? role.DefaultTools : null
        };
    }
}
