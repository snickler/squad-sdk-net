namespace Squad.SDK.NET.Storage;

public class StorageError : Exception
{
    public string? Key { get; }

    public StorageError(string message, string? key = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Key = key;
    }
}

public sealed class ProviderError : StorageError
{
    public ProviderError(string message, string? key = null, Exception? innerException = null)
        : base(message, key, innerException) { }
}

public sealed class StorageFullError : StorageError
{
    public StorageFullError(string message, string? key = null)
        : base(message, key) { }
}

public sealed class AccessDeniedError : StorageError
{
    public AccessDeniedError(string message, string? key = null)
        : base(message, key) { }
}
