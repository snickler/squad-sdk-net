using Squad.SDK.NET.Coordinator;

namespace Squad.SDK.NET.Agents;

public sealed record AgentSessionInfo
{
    public required AgentCharter Charter { get; init; }
    public string? SessionId { get; set; }
    public AgentState State { get; set; } = AgentState.Pending;
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? LastActiveAt { get; set; }
    public ResponseTier ResponseMode { get; set; } = ResponseTier.Standard;
    
    public string? ParentAgentName { get; set; }
    public List<string> SubAgentNames { get; set; } = [];
    public int Depth { get; set; }
}
