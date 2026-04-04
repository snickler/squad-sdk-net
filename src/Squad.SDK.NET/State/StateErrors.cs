namespace Squad.SDK.NET.State;

public class StateError : Exception
{
    public StateError(string message, Exception? innerException = null) : base(message, innerException) { }
}

public sealed class NotFoundError : StateError
{
    public string EntityType { get; }
    public string Key { get; }

    public NotFoundError(string entityType, string key)
        : base($"{entityType} '{key}' not found.")
    {
        EntityType = entityType;
        Key = key;
    }
}

public sealed class ParseError : StateError
{
    public string? FilePath { get; }

    public ParseError(string message, string? filePath = null, Exception? innerException = null)
        : base(message, innerException)
    {
        FilePath = filePath;
    }
}

public sealed class WriteConflictError : StateError
{
    public string Key { get; }

    public WriteConflictError(string key)
        : base($"Write conflict for key '{key}'.")
    {
        Key = key;
    }
}
