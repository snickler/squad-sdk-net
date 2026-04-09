using Microsoft.Extensions.Logging;
using Squad.SDK.NET;
using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Events;

// ── Guard: GITHUB_TOKEN is required for live LLM mode ────────────────────────

var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

Console.WriteLine("🎭 Knock-Knock Comedy Hour — Squad SDK beginner sample (.NET)");
Console.WriteLine();

if (string.IsNullOrWhiteSpace(token))
{
    Console.WriteLine("⚠️  GITHUB_TOKEN is not set. Running in demo mode.");
    Console.WriteLine("   Set GITHUB_TOKEN and re-run for live LLM responses.");
    Console.WriteLine();
    RunDemoMode();
    return;
}

// ── Live LLM mode ─────────────────────────────────────────────────────────────

using var loggerFactory = LoggerFactory.Create(b =>
    b.AddConsole().SetMinimumLevel(LogLevel.Warning));

await using var client = new SquadClient(loggerFactory);

Console.WriteLine("   McManus (Teller) vs. Fenster (Responder)");
Console.WriteLine();
Console.WriteLine("   Connecting to Copilot...");

try
{
    await client.StartAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"   ❌ Could not connect: {ex.Message}");
    Console.WriteLine("   Falling back to demo mode.");
    Console.WriteLine();
    RunDemoMode();
    return;
}

Console.WriteLine("   ✓ Connected. Let the jokes begin!");
Console.WriteLine();

ISquadSession? tellerSession = null;
ISquadSession? responderSession = null;

try
{
    tellerSession = await client.CreateSessionAsync(new SquadSessionConfig
    {
        ClientName    = "McManus",
        SystemMessage = "You are McManus, a quick-witted comedian. Tell knock-knock jokes. Keep each turn short."
    });

    responderSession = await client.CreateSessionAsync(new SquadSessionConfig
    {
        ClientName    = "Fenster",
        SystemMessage = "You are Fenster, the audience for knock-knock jokes. Respond naturally and briefly."
    });
}
catch (Exception ex)
{
    Console.WriteLine($"   ❌ Could not create sessions: {ex.Message}");
    Console.WriteLine("   Falling back to demo mode.");
    if (tellerSession is not null) await tellerSession.DisposeAsync();
    if (responderSession is not null) await responderSession.DisposeAsync();
    await client.StopAsync();
    RunDemoMode();
    return;
}

await using var _ = tellerSession;
await using var __ = responderSession;

try
{
    for (var round = 1; round <= 3; round++)
    {
        Console.WriteLine($"── Round {round} ───────────────────────────────────────────────");

        using var sub1 = tellerSession.On(evt =>
        {
            if (evt.Type == SquadEventType.MessageDelta && evt.Payload is StreamDeltaPayload d)
                Console.Write(d.Content);
            else if (evt.Type == SquadEventType.SessionIdle)
                Console.WriteLine();
        });

        using var sub2 = responderSession.On(evt =>
        {
            if (evt.Type == SquadEventType.MessageDelta && evt.Payload is StreamDeltaPayload d)
                Console.Write(d.Content);
            else if (evt.Type == SquadEventType.SessionIdle)
                Console.WriteLine();
        });

        Console.Write("🎭 McManus: ");
        await tellerSession.SendAndWaitAsync(new SquadMessageOptions { Prompt = "Say only 'Knock knock!'" });
        Console.Write("🎭 Fenster: ");
        await responderSession.SendAndWaitAsync(new SquadMessageOptions { Prompt = "Who's there?" });
        Console.Write("🎭 McManus: ");
        await tellerSession.SendAndWaitAsync(new SquadMessageOptions { Prompt = "Give the setup word only." });
        Console.Write("🎭 Fenster: ");
        await responderSession.SendAndWaitAsync(new SquadMessageOptions { Prompt = "Ask '<setup> who?'" });
        Console.Write("🎭 McManus: ");
        await tellerSession.SendAndWaitAsync(new SquadMessageOptions { Prompt = "Deliver the punchline." });

        Console.WriteLine();
    }

    Console.WriteLine("Thanks for laughing with the Squad! 🎉");
}
catch (Exception ex)
{
    Console.WriteLine($"\n   ❌ Session error: {ex.Message}");
    Console.WriteLine("   (Ensure GITHUB_TOKEN is valid and Copilot is enabled on your account.)");
}

await client.StopAsync();

static void RunDemoMode()
{
    Console.WriteLine("🎭 Knock-Knock Comedy Hour (Demo Mode)");
    Console.WriteLine();
    Console.WriteLine("   McManus (Teller) vs. Fenster (Responder)");
    Console.WriteLine();

    string[] lines =
    [
        "🎭 McManus: Knock knock!",
        "🎭 Fenster: Who's there?",
        "🎭 McManus: TypeScript.",
        "🎭 Fenster: TypeScript who?",
        "🎭 McManus: TypeScript checking your jokes for type safety! 🔍",
        "",
        "🎭 McManus: Knock knock!",
        "🎭 Fenster: Who's there?",
        "🎭 McManus: .NET.",
        "🎭 Fenster: .NET who?",
        "🎭 McManus: .NET catch you slacking — exceptions everywhere! 🚨",
        "",
        "🎭 McManus: Knock knock!",
        "🎭 Fenster: Who's there?",
        "🎭 McManus: NuGet.",
        "🎭 Fenster: NuGet who?",
        "🎭 McManus: NuGet outta here and ship the release already! 📦",
    ];

    foreach (var line in lines)
        Console.WriteLine(line);

    Console.WriteLine();
    Console.WriteLine("Set GITHUB_TOKEN to run with live LLM responses.");
}
