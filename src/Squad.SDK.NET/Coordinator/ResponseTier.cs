namespace Squad.SDK.NET.Coordinator;

/// <summary>
/// Defines the quality tier for a routing response, controlling how much processing an agent applies.
/// </summary>
public enum ResponseTier
{
    /// <summary>A trivial canned response that bypasses agent processing entirely.</summary>
    Direct,

    /// <summary>A quick, low-cost response suitable for simple queries.</summary>
    Lightweight,

    /// <summary>A balanced response with standard agent processing.</summary>
    Standard,

    /// <summary>A comprehensive response with maximum agent depth and reasoning.</summary>
    Full
}
