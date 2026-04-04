namespace Squad.SDK.NET.Coordinator;

public static class DirectResponse
{
    private static readonly (string[] Patterns, string Response)[] _mappings =
    [
        (["hello", "hi", "hey", "greetings"], "Hello! I'm Squad, your AI development team. How can I help you today?"),
        (["help", "what can you do", "capabilities", "commands"], "I can help you with feature development, bug fixes, testing, documentation, refactoring, architecture, and research tasks. Just describe what you need!"),
        (["version", "what version"], "Squad SDK v1.0 — powered by GitHub Copilot multi-agent orchestration."),
        (["status", "health", "ping"], "Squad is online and ready."),
        (["thanks", "thank you", "ty", "cheers"], "You're welcome! Let me know if there's anything else I can help with."),
        (["bye", "goodbye", "exit", "quit"], "Goodbye! Come back anytime."),
    ];

    public static bool TryGetDirectResponse(string message, out string? response)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            response = null;
            return false;
        }

        // Strip punctuation for flexible matching
        var normalized = new string(message.Trim().ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());

        foreach (var (patterns, reply) in _mappings)
        {
            foreach (var pattern in patterns)
            {
                if (normalized == pattern
                    || normalized.StartsWith(pattern + " ")
                    || normalized.EndsWith(" " + pattern)
                    || normalized.Contains(" " + pattern + " "))
                {
                    response = reply;
                    return true;
                }
            }
        }

        response = null;
        return false;
    }
}
