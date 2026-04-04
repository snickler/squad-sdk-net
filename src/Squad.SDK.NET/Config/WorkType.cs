namespace Squad.SDK.NET.Config;

/// <summary>Defines well-known work type constants used in routing rules.</summary>
/// <seealso cref="RoutingRule"/>
public static class WorkType
{
    /// <summary>Feature development work.</summary>
    public const string FeatureDev = "feature-dev";

    /// <summary>Bug fix work.</summary>
    public const string BugFix = "bug-fix";

    /// <summary>Testing and quality assurance work.</summary>
    public const string Testing = "testing";

    /// <summary>Documentation authoring work.</summary>
    public const string Documentation = "documentation";

    /// <summary>Code refactoring work.</summary>
    public const string Refactoring = "refactoring";

    /// <summary>Architecture and design work.</summary>
    public const string Architecture = "architecture";

    /// <summary>Research and investigation work.</summary>
    public const string Research = "research";

    /// <summary>Issue triage work.</summary>
    public const string Triage = "triage";

    /// <summary>Meta or administrative work.</summary>
    public const string Meta = "meta";
}
