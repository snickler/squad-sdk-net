namespace Squad.SDK.NET.Builder;

/// <summary>
/// Exception thrown when a builder detects validation errors during <c>Build()</c>.
/// </summary>
public sealed class BuilderValidationError : InvalidOperationException
{
    /// <summary>Gets the name of the builder that failed validation.</summary>
    public string BuilderName { get; }

    /// <summary>Gets the list of validation error messages.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Initializes a new instance with multiple validation errors.
    /// </summary>
    /// <param name="builderName">The name of the builder that failed.</param>
    /// <param name="errors">The validation errors.</param>
    public BuilderValidationError(string builderName, IReadOnlyList<string> errors)
        : base($"{builderName} validation failed: {string.Join("; ", errors)}")
    {
        BuilderName = builderName;
        Errors = errors;
    }

    /// <summary>
    /// Initializes a new instance with a single validation error.
    /// </summary>
    /// <param name="builderName">The name of the builder that failed.</param>
    /// <param name="error">The validation error message.</param>
    public BuilderValidationError(string builderName, string error)
        : this(builderName, new[] { error })
    {
    }
}
