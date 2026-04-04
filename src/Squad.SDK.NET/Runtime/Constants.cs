namespace Squad.SDK.NET.Runtime;

public static class Constants
{
    public static class Models
    {
        public const string Gpt5 = "gpt-5";
        public const string Gpt5Mini = "gpt-5-mini";
        public const string ClaudeOpus = "claude-opus-4.6";
        public const string ClaudeSonnet = "claude-sonnet-4.6";
        public const string ClaudeHaiku = "claude-haiku-4.5";
    }

    public static class Timeouts
    {
        public static readonly TimeSpan SessionCreate = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan MessageSend = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan AgentSpawn = TimeSpan.FromSeconds(60);
        public static readonly TimeSpan Shutdown = TimeSpan.FromSeconds(15);
    }

    public static class AgentRoles
    {
        public const string Lead = "lead";
        public const string Frontend = "frontend";
        public const string Backend = "backend";
        public const string Tester = "tester";
        public const string Scribe = "scribe";
    }
}
