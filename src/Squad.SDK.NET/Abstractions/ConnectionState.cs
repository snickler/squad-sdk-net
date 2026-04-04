namespace Squad.SDK.NET.Abstractions;

/// <summary>
/// Represents the connection state of an <see cref="ISquadClient"/>.
/// </summary>
public enum ConnectionState
{
    /// <summary>The client is not connected.</summary>
    Disconnected,

    /// <summary>The client is establishing a connection.</summary>
    Connecting,

    /// <summary>The client is connected and ready.</summary>
    Connected,

    /// <summary>The client lost its connection and is attempting to reconnect.</summary>
    Reconnecting,

    /// <summary>The client encountered an unrecoverable connection error.</summary>
    Error
}
