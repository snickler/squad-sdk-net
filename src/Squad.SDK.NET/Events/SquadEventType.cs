namespace Squad.SDK.NET.Events;

/// <summary>
/// Identifies the type of a <see cref="SquadEvent"/>.
/// </summary>
public enum SquadEventType
{
    /// <summary>A new session was created.</summary>
    SessionCreated,

    /// <summary>A session has become idle with no pending work.</summary>
    SessionIdle,

    /// <summary>A session encountered an error.</summary>
    SessionError,

    /// <summary>A session was destroyed.</summary>
    SessionDestroyed,

    /// <summary>A complete message was received from the assistant.</summary>
    SessionMessage,

    /// <summary>A tool call was started or completed within a session.</summary>
    SessionToolCall,

    /// <summary>An agent reached a defined milestone.</summary>
    AgentMilestone,

    /// <summary>The coordinator made a routing decision.</summary>
    CoordinatorRouting,

    /// <summary>The agent pool reported a health status update.</summary>
    PoolHealth,

    /// <summary>A streaming message delta was received.</summary>
    MessageDelta,

    /// <summary>Token usage information was reported.</summary>
    Usage,

    /// <summary>A streaming reasoning delta was received.</summary>
    ReasoningDelta
}
