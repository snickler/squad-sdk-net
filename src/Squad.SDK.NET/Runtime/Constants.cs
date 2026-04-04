namespace Squad.SDK.NET.Runtime;

/// <summary>
/// Static constants for model names, timeouts, and agent roles used throughout the SDK.
/// </summary>
public static class Constants
{
    /// <summary>Well-known model identifiers.</summary>
    public static class Models
    {
        /// <summary>GPT-5 model identifier.</summary>
        public const string Gpt5 = "gpt-5";
        /// <summary>GPT-5 Mini model identifier.</summary>
        public const string Gpt5Mini = "gpt-5-mini";
        /// <summary>Claude Opus 4.6 model identifier.</summary>
        public const string ClaudeOpus = "claude-opus-4.6";
        /// <summary>Claude Sonnet 4.6 model identifier.</summary>
        public const string ClaudeSonnet = "claude-sonnet-4.6";
        /// <summary>Claude Haiku 4.5 model identifier.</summary>
        public const string ClaudeHaiku = "claude-haiku-4.5";
    }

    /// <summary>Default timeout durations for various SDK operations.</summary>
    public static class Timeouts
    {
        /// <summary>Timeout for creating a new session.</summary>
        public static readonly TimeSpan SessionCreate = TimeSpan.FromSeconds(30);
        /// <summary>Timeout for sending a message.</summary>
        public static readonly TimeSpan MessageSend = TimeSpan.FromMinutes(5);
        /// <summary>Timeout for spawning a new agent.</summary>
        public static readonly TimeSpan AgentSpawn = TimeSpan.FromSeconds(60);
        /// <summary>Timeout for graceful shutdown.</summary>
        public static readonly TimeSpan Shutdown = TimeSpan.FromSeconds(15);
    }

    /// <summary>Well-known agent role identifiers.</summary>
    public static class AgentRoles
    {
        /// <summary>Team lead role.</summary>
        public const string Lead = "lead";
        /// <summary>Frontend engineer role.</summary>
        public const string Frontend = "frontend";
        /// <summary>Backend engineer role.</summary>
        public const string Backend = "backend";
        /// <summary>Quality engineer / tester role.</summary>
        public const string Tester = "tester";
        /// <summary>Technical writer / scribe role.</summary>
        public const string Scribe = "scribe";
    }
}
