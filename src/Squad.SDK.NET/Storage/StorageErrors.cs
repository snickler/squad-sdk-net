namespace Squad.SDK.NET.Storage;

/// <summary>
/// Base exception for storage-related errors.
/// </summary>
public class StorageError : Exception
{
    /// <summary>Gets the storage key associated with the error, if any.</summary>
    public string? Key { get; }

    /// <summary>
    /// Initializes a new <see cref="StorageError"/>.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="key">Optional storage key related to the error.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public StorageError(string message, string? key = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Key = key;
    }
}

/// <summary>
/// Thrown when the underlying storage provider encounters an error.
/// </summary>
public sealed class ProviderError : StorageError
{
    /// <summary>
    /// Initializes a new <see cref="ProviderError"/>.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="key">Optional storage key.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public ProviderError(string message, string? key = null, Exception? innerException = null)
        : base(message, key, innerException) { }
}

/// <summary>
/// Thrown when the storage is full and cannot accept additional data.
/// </summary>
public sealed class StorageFullError : StorageError
{
    /// <summary>
    /// Initializes a new <see cref="StorageFullError"/>.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="key">Optional storage key.</param>
    public StorageFullError(string message, string? key = null)
        : base(message, key) { }
}

/// <summary>
/// Thrown when access to the storage location is denied.
/// </summary>
public sealed class AccessDeniedError : StorageError
{
    /// <summary>
    /// Initializes a new <see cref="AccessDeniedError"/>.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="key">Optional storage key.</param>
    public AccessDeniedError(string message, string? key = null)
        : base(message, key) { }
}
