namespace Squad.SDK.NET.State;

/// <summary>
/// Base exception for state management errors.
/// </summary>
public class StateError : Exception
{
    /// <summary>
    /// Initializes a new <see cref="StateError"/>.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public StateError(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a requested entity is not found in state.
/// </summary>
public sealed class NotFoundError : StateError
{
    /// <summary>Gets the type of entity that was not found.</summary>
    public string EntityType { get; }
    /// <summary>Gets the key that was looked up.</summary>
    public string Key { get; }

    /// <summary>
    /// Initializes a new <see cref="NotFoundError"/>.
    /// </summary>
    /// <param name="entityType">The entity type (e.g., "agent", "decision").</param>
    /// <param name="key">The key that was not found.</param>
    public NotFoundError(string entityType, string key)
        : base($"{entityType} '{key}' not found.")
    {
        EntityType = entityType;
        Key = key;
    }
}

/// <summary>
/// Thrown when state data cannot be parsed (e.g., corrupted JSON).
/// </summary>
public sealed class ParseError : StateError
{
    /// <summary>Gets the optional file path that failed to parse.</summary>
    public string? FilePath { get; }

    /// <summary>
    /// Initializes a new <see cref="ParseError"/>.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="filePath">Optional file path.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public ParseError(string message, string? filePath = null, Exception? innerException = null)
        : base(message, innerException)
    {
        FilePath = filePath;
    }
}

/// <summary>
/// Thrown when a concurrent write conflict is detected for a state key.
/// </summary>
public sealed class WriteConflictError : StateError
{
    /// <summary>Gets the key involved in the conflict.</summary>
    public string Key { get; }

    /// <summary>
    /// Initializes a new <see cref="WriteConflictError"/>.
    /// </summary>
    /// <param name="key">The state key.</param>
    public WriteConflictError(string key)
        : base($"Write conflict for key '{key}'.")
    {
        Key = key;
    }
}
