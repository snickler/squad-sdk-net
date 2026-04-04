namespace Squad.SDK.NET.Builder;

public sealed class BuilderValidationError : InvalidOperationException
{
    public string BuilderName { get; }
    public IReadOnlyList<string> Errors { get; }

    public BuilderValidationError(string builderName, IReadOnlyList<string> errors)
        : base($"{builderName} validation failed: {string.Join("; ", errors)}")
    {
        BuilderName = builderName;
        Errors = errors;
    }

    public BuilderValidationError(string builderName, string error)
        : this(builderName, new[] { error })
    {
    }
}
