using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Hooks;

Console.WriteLine("shield  hook-governance - Squad SDK governance hooks sample (.NET)");
Console.WriteLine();

using var loggerFactory = LoggerFactory.Create(b =>
    b.AddConsole().SetMinimumLevel(LogLevel.Warning));

// Demo 1: File-Write Guards
PrintStep("Demo 1 - File-Write Guards");
Console.WriteLine("  Restricts file writes to an approved set of paths.");
Console.WriteLine();

var writePolicy = new PolicyConfig { AllowedWritePaths = ["src/", "tests/"] };
var writePipeline = new HookPipeline(writePolicy);

(string Path, string Tool)[] writeAttempts =
[
    ("src/utils/helper.cs",   "write_file"),
    ("tests/MyTest.cs",       "write_file"),
    ("/etc/passwd",           "write_file"),
    ("C:\\Windows\\System32", "write_file"),
];

foreach (var (path, tool) in writeAttempts)
{
    var ctx = new PreToolUseContext
    {
        ToolName  = tool,
        SessionId = "session-1",
        AgentName = "backend",
        Arguments = new Dictionary<string, object?> { ["path"] = path }
    };
    var result = await writePipeline.RunPreToolHooksAsync(ctx);
    var icon   = result.Action == HookAction.Allow ? "allow [OK]" : "block [X]";
    Console.WriteLine($"  Write to {path,-40} {icon}");
    if (result.Action == HookAction.Block)
        Console.WriteLine($"             => {result.Reason}");
}
Console.WriteLine();

// Demo 2: PII Scrubbing
PrintStep("Demo 2 - PII Scrubbing");
Console.WriteLine("  Automatically redacts emails, phone numbers, and SSNs from tool output.");
Console.WriteLine();

string[] samples =
[
    "Deploy fix by brady@example.com -- cc: alice@company.io",
    "Contact support at 555-867-5309 or email help@example.com",
    "SSN on file: 123-45-6789",
];

foreach (var input in samples)
{
    var scrubbed = PiiScrubberHook.ScrubPii(input);
    Console.WriteLine($"  Before: {input}");
    Console.WriteLine($"  After:  {scrubbed}");
    Console.WriteLine();
}

// Demo 3: Reviewer Lockout
PrintStep("Demo 3 - Reviewer Lockout");
Console.WriteLine("  A reviewer that rejects a PR is locked out of the reviewed file.");
Console.WriteLine();

var lockoutLogger   = loggerFactory.CreateLogger<ReviewerLockoutHook>();
var lockoutHook     = new ReviewerLockoutHook(lockoutLogger);
var lockoutPipeline = new HookPipeline();
lockoutPipeline.AddPreToolHook(lockoutHook.CreateHook());
lockoutHook.Lockout("src/auth.cs", "backend");

(string Agent, string File)[] editAttempts =
[
    ("backend",  "src/auth.cs"),
    ("frontend", "src/auth.cs"),
    ("backend",  "src/helpers.cs"),
];

foreach (var (agent, file) in editAttempts)
{
    var ctx = new PreToolUseContext
    {
        ToolName  = "write_file",
        SessionId = "session-lockout",
        AgentName = agent,
        Arguments = new Dictionary<string, object?> { ["path"] = file }
    };
    var result = await lockoutPipeline.RunPreToolHooksAsync(ctx);
    var icon   = result.Action == HookAction.Allow ? "allow [OK]" : "block [X]";
    Console.WriteLine($"  {agent,-10} edits {file,-22} {icon}");
    if (result.Action == HookAction.Block)
        Console.WriteLine($"             => {result.Reason}");
}
Console.WriteLine();

// Demo 4: Ask-User Rate Limiter
PrintStep("Demo 4 - Ask-User Rate Limiter");
Console.WriteLine("  Caps the number of times an agent can prompt the user per session.");
Console.WriteLine();

var ratePolicy   = new PolicyConfig { MaxAskUserPerSession = 3 };
var ratePipeline = new HookPipeline(ratePolicy);

for (var i = 1; i <= 5; i++)
{
    var ctx = new PreToolUseContext
    {
        ToolName  = "ask_user",
        SessionId = "session-rate",
        AgentName = "coder",
        Arguments = new Dictionary<string, object?> { ["question"] = $"question #{i}" }
    };
    var result = await ratePipeline.RunPreToolHooksAsync(ctx);
    var icon   = result.Action == HookAction.Allow ? "allow [OK]" : "block [X]";
    Console.WriteLine($"    Ask #{i}: {icon}");
    if (result.Action == HookAction.Block)
        Console.WriteLine($"          => {result.Reason}");
}
Console.WriteLine();

static void PrintStep(string title)
{
    Console.WriteLine(new string('-', 60));
    Console.WriteLine($"  {title}");
    Console.WriteLine(new string('-', 60));
}
