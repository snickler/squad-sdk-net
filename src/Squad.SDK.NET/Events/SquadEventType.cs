namespace Squad.SDK.NET.Events;

public enum SquadEventType
{
    SessionCreated,
    SessionIdle,
    SessionError,
    SessionDestroyed,
    SessionMessage,
    SessionToolCall,
    AgentMilestone,
    CoordinatorRouting,
    PoolHealth,
    MessageDelta,
    Usage,
    ReasoningDelta
}
