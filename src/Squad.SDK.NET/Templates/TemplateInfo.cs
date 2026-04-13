namespace Squad.SDK.NET.Templates;

/// <summary>
/// Metadata describing an embedded SDK template bundled in the assembly.
/// </summary>
/// <remarks>
/// For templates in subdirectories, <see cref="Name"/> includes the relative path
/// using forward-slash separators (e.g., <c>agents/charter.md.template</c>).
/// Use <see cref="OutputPath"/> for a path-preserving extraction target, or
/// <see cref="OutputFileName"/> for just the leaf file name.
/// </remarks>
/// <seealso cref="TemplateProvider"/>
public sealed record TemplateInfo
{
    /// <summary>
    /// Gets the template identifier relative to the templates root.
    /// For root-level templates this is a simple file name (e.g., <c>squad.agent.md.template</c>).
    /// For nested templates this includes the relative path with forward-slash separators
    /// (e.g., <c>agents/charter.md.template</c>).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>Gets the full embedded resource name used to load the template from the assembly.</summary>
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the suggested relative output path when extracting, preserving directory structure.
    /// Strips the <c>.template</c> suffix when present
    /// (e.g., <c>squad.agent.md</c> or <c>agents/charter.md</c>).
    /// </summary>
    public string OutputPath =>
        Name.EndsWith(".template", StringComparison.OrdinalIgnoreCase)
            ? Name[..^".template".Length]
            : Name;

    /// <summary>
    /// Gets the suggested output file name (leaf name only, no directory components) when extracting.
    /// Strips the <c>.template</c> suffix when present (e.g., <c>squad.agent.md</c>).
    /// </summary>
    public string OutputFileName
    {
        get
        {
            var path = OutputPath;
            var lastSep = path.LastIndexOf('/');
            return lastSep >= 0 ? path[(lastSep + 1)..] : path;
        }
    }
}
