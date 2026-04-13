using System.Reflection;

namespace Squad.SDK.NET.Templates;

/// <summary>
/// Provides AOT-safe access to embedded SDK templates bundled in the assembly.
/// Use <see cref="EnumerateTemplates()"/> to discover available templates and
/// <see cref="ExtractAsync(string, string, bool, CancellationToken)"/> to write a template to a target path.
/// </summary>
/// <remarks>
/// <para>
/// Templates are embedded as assembly resources at build time. New templates are
/// automatically discovered when added to the <c>Templates/</c> directory — no code
/// changes required. Subdirectory structure is preserved in template names using
/// forward-slash separators (e.g., <c>agents/charter.md.template</c>).
/// </para>
/// <para>
/// Use <see cref="EnumerateTemplates(string)"/> to list templates under a specific
/// subtree, and <see cref="ExtractToDirectoryAsync(string, bool, CancellationToken)"/>
/// to extract all templates while preserving directory structure.
/// </para>
/// </remarks>
public static class TemplateProvider
{
    /// <summary>The well-known name for the Copilot Custom Agent declaration template.</summary>
    public const string SquadAgentTemplateName = "squad.agent.md.template";

    /// <summary>The conventional directory within a repository for Copilot Custom Agent declarations.</summary>
    public const string AgentDeclarationDirectory = ".github/agents";

    /// <summary>The conventional file name for the Squad agent declaration.</summary>
    public const string AgentDeclarationFileName = "squad.agent.md";

    /// <summary>
    /// Prefix used to identify embedded template resources within the assembly.
    /// </summary>
    internal const string ResourcePrefix = "Squad.SDK.NET.Templates.";

    private static readonly Assembly ResourceAssembly = typeof(TemplateProvider).Assembly;

    /// <summary>
    /// Scaffolds the bundled Squad agent declaration template to the conventional
    /// repository location at <c>{repoRoot}/.github/agents/squad.agent.md</c>,
    /// mirroring the upstream <c>squad install</c> behavior.
    /// </summary>
    /// <remarks>
    /// This is the .NET equivalent of the upstream <c>squad install</c> step that materializes
    /// the Copilot Custom Agent declaration into the repository. The <c>.github/agents/</c>
    /// directory is created automatically if it does not exist.
    /// </remarks>
    /// <param name="repoRoot">Absolute path to the repository root directory.</param>
    /// <param name="overwrite">
    /// When <see langword="true"/>, replaces an existing declaration file.
    /// When <see langword="false"/> (the default), throws <see cref="IOException"/> if the file already exists.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The full path of the written agent declaration file.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="repoRoot"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the bundled agent template is not found or cannot be opened.</exception>
    /// <exception cref="IOException">Thrown when the target file already exists and <paramref name="overwrite"/> is <see langword="false"/>.</exception>
    public static async Task<string> ScaffoldAgentDeclarationAsync(
        string repoRoot,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repoRoot);

        var targetPath = Path.Combine(repoRoot, AgentDeclarationDirectory, AgentDeclarationFileName);
        await ExtractAsync(SquadAgentTemplateName, targetPath, overwrite, cancellationToken).ConfigureAwait(false);
        return targetPath;
    }

    /// <summary>
    /// Enumerates all embedded templates bundled in the SDK assembly.
    /// </summary>
    /// <returns>A read-only list of <see cref="TemplateInfo"/> describing each available template.</returns>
    public static IReadOnlyList<TemplateInfo> EnumerateTemplates()
    {
        var names = ResourceAssembly.GetManifestResourceNames();
        var templates = new List<TemplateInfo>();

        foreach (var name in names)
        {
            if (!name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
                continue;

            var templateName = NormalizePath(name[ResourcePrefix.Length..]);
            if (string.IsNullOrEmpty(templateName))
                continue;

            templates.Add(new TemplateInfo
            {
                Name = templateName,
                ResourceName = name
            });
        }

        return templates.AsReadOnly();
    }

    /// <summary>
    /// Enumerates embedded templates whose <see cref="TemplateInfo.Name"/> starts with
    /// the specified path prefix, enabling subtree queries.
    /// </summary>
    /// <param name="pathPrefix">
    /// The forward-slash-delimited prefix to match (e.g., <c>agents/</c>).
    /// Pass an empty string or <see langword="null"/> to enumerate all templates.
    /// </param>
    /// <returns>A read-only list of matching <see cref="TemplateInfo"/> records.</returns>
    /// <example>
    /// <code>
    /// // List only templates under the "skills/" subtree
    /// var skillTemplates = TemplateProvider.EnumerateTemplates("skills/");
    /// </code>
    /// </example>
    public static IReadOnlyList<TemplateInfo> EnumerateTemplates(string? pathPrefix)
    {
        if (string.IsNullOrEmpty(pathPrefix))
            return EnumerateTemplates();

        var normalized = NormalizePath(pathPrefix);
        var all = EnumerateTemplates();
        var filtered = new List<TemplateInfo>();

        foreach (var template in all)
        {
            if (template.Name.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
                filtered.Add(template);
        }

        return filtered.AsReadOnly();
    }

    /// <summary>
    /// Gets metadata for a specific template by name.
    /// </summary>
    /// <param name="templateName">
    /// The template identifier relative to the templates root (e.g., <c>squad.agent.md.template</c>
    /// or <c>agents/charter.md.template</c> for nested templates).
    /// Use <see cref="SquadAgentTemplateName"/> for the bundled agent template.
    /// </param>
    /// <returns>The <see cref="TemplateInfo"/> if found; otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="templateName"/> is null or whitespace.</exception>
    public static TemplateInfo? GetTemplate(string templateName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);

        var normalized = NormalizePath(templateName);
        var resourceName = ResourcePrefix + normalized;
        var info = ResourceAssembly.GetManifestResourceInfo(resourceName);
        if (info is null)
            return null;

        return new TemplateInfo
        {
            Name = normalized,
            ResourceName = resourceName
        };
    }

    /// <summary>
    /// Extracts an embedded template to the specified target file path.
    /// </summary>
    /// <param name="templateName">
    /// The template identifier (e.g., <c>squad.agent.md.template</c>
    /// or <c>agents/charter.md.template</c>).
    /// Use <see cref="SquadAgentTemplateName"/> for the bundled agent template.
    /// </param>
    /// <param name="targetPath">The full file path where the template will be written.</param>
    /// <param name="overwrite">When <see langword="true"/>, overwrites an existing file at <paramref name="targetPath"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous extraction operation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="templateName"/> or <paramref name="targetPath"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the template is not found in the assembly.</exception>
    /// <exception cref="IOException">Thrown when <paramref name="targetPath"/> already exists and <paramref name="overwrite"/> is <see langword="false"/>.</exception>
    public static async Task ExtractAsync(
        string templateName,
        string targetPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

        var template = GetTemplate(templateName)
            ?? throw new InvalidOperationException($"Template '{templateName}' was not found in the assembly.");

        await ExtractAsync(template, targetPath, overwrite, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Extracts an embedded template to the specified target file path.
    /// </summary>
    /// <param name="template">The template metadata obtained from <see cref="EnumerateTemplates()"/> or <see cref="GetTemplate"/>.</param>
    /// <param name="targetPath">The full file path where the template will be written.</param>
    /// <param name="overwrite">When <see langword="true"/>, overwrites an existing file at <paramref name="targetPath"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous extraction operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="template"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="targetPath"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the embedded resource stream cannot be opened.</exception>
    /// <exception cref="IOException">Thrown when <paramref name="targetPath"/> already exists and <paramref name="overwrite"/> is <see langword="false"/>.</exception>
    public static async Task ExtractAsync(
        TemplateInfo template,
        string targetPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

        if (!overwrite && File.Exists(targetPath))
            throw new IOException($"File already exists at '{targetPath}'. Set overwrite to true to replace it.");

        using var stream = ResourceAssembly.GetManifestResourceStream(template.ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{template.ResourceName}' could not be opened.");

        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var fileMode = overwrite ? FileMode.Create : FileMode.CreateNew;
        await using var fileStream = new FileStream(
            targetPath, fileMode, FileAccess.Write, FileShare.None,
            bufferSize: 4096, useAsync: true);
        await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Extracts all embedded templates to the specified target directory, preserving the
    /// relative directory structure of nested templates. Each template's
    /// <see cref="TemplateInfo.OutputPath"/> determines its relative location under
    /// <paramref name="targetDirectory"/>.
    /// </summary>
    /// <param name="targetDirectory">The root directory where templates will be extracted.</param>
    /// <param name="overwrite">When <see langword="true"/>, overwrites existing files.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of full file paths that were written.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="targetDirectory"/> is null or whitespace.</exception>
    /// <exception cref="IOException">Thrown when a target file already exists and <paramref name="overwrite"/> is <see langword="false"/>.</exception>
    public static async Task<IReadOnlyList<string>> ExtractToDirectoryAsync(
        string targetDirectory,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);

        var templates = EnumerateTemplates();
        return await ExtractTemplatesToDirectoryAsync(
            templates, targetDirectory, overwrite, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Extracts embedded templates matching <paramref name="pathPrefix"/> to the specified
    /// target directory, preserving relative directory structure.
    /// </summary>
    /// <param name="targetDirectory">The root directory where templates will be extracted.</param>
    /// <param name="pathPrefix">
    /// The forward-slash-delimited prefix to match (e.g., <c>agents/</c>).
    /// Only templates whose <see cref="TemplateInfo.Name"/> starts with this prefix are extracted.
    /// </param>
    /// <param name="overwrite">When <see langword="true"/>, overwrites existing files.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of full file paths that were written.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="targetDirectory"/> or <paramref name="pathPrefix"/> is null or whitespace.</exception>
    /// <exception cref="IOException">Thrown when a target file already exists and <paramref name="overwrite"/> is <see langword="false"/>.</exception>
    public static async Task<IReadOnlyList<string>> ExtractToDirectoryAsync(
        string targetDirectory,
        string pathPrefix,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(pathPrefix);

        var templates = EnumerateTemplates(pathPrefix);
        return await ExtractTemplatesToDirectoryAsync(
            templates, targetDirectory, overwrite, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Normalizes path separators in a template name to forward slashes.
    /// </summary>
    internal static string NormalizePath(string path)
        => path.Replace('\\', '/');

    private static async Task<IReadOnlyList<string>> ExtractTemplatesToDirectoryAsync(
        IReadOnlyList<TemplateInfo> templates,
        string targetDirectory,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        var writtenPaths = new List<string>(templates.Count);

        foreach (var template in templates)
        {
            // Convert forward-slash OutputPath to OS path separators for file system operations
            var relativePath = template.OutputPath.Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(targetDirectory, relativePath);

            await ExtractAsync(template, fullPath, overwrite, cancellationToken).ConfigureAwait(false);
            writtenPaths.Add(fullPath);
        }

        return writtenPaths.AsReadOnly();
    }
}
