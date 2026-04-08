using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Casting;
using Squad.SDK.NET.Config;
using Squad.SDK.NET.Resolution;

Console.WriteLine("🎬 hello-squad — Squad SDK beginner sample (.NET)");
Console.WriteLine();

using var loggerFactory = LoggerFactory.Create(b =>
    b.AddConsole().SetMinimumLevel(LogLevel.Warning));

// ── Step 1: Resolve .squad/ directory ────────────────────────────────────────

PrintStep("Step 1 — Resolve .squad/ directory");

var tempDir = Path.Combine(Path.GetTempPath(), $"hello-squad-demo-{Guid.NewGuid():N}");
var squadDir = Path.Combine(tempDir, ".squad");
Directory.CreateDirectory(squadDir);

var paths = SquadResolver.ResolveSquad(tempDir);
Console.WriteLine($"  ✅ Created demo .squad/ at: {squadDir}");
Console.WriteLine($"     ResolveSquad() → {paths?.ProjectDir ?? "(not found)"}");
Console.WriteLine();

// ── Step 2: Cast a team from "The Usual Suspects" ────────────────────────────

PrintStep("Step 2 — Cast a team from \"The Usual Suspects\"");

var castingConfig = new CastingConfig
{
    AllowlistUniverses = ["The Usual Suspects"]
};

var engine = new CastingEngine(castingConfig, loggerFactory.CreateLogger<CastingEngine>());

(string AgentName, string RoleId)[] roster =
[
    ("Keyser",  "lead"),
    ("McManus", "developer"),
    ("Fenster", "tester"),
    ("Verbal",  "scribe")
];

Console.WriteLine("  Universe: The Usual Suspects");
Console.WriteLine($"  Team size: {roster.Length}");
Console.WriteLine();

var members = new List<(string AgentName, string RoleId, CastMember Member)>();
foreach (var (agentName, roleId) in roster)
{
    var member = engine.Cast(agentName, roleId, "The Usual Suspects");
    members.Add((agentName, roleId, member));
}

var lead = members.First(m => m.RoleId == "lead");
Console.WriteLine($"  🎭 {lead.AgentName} — Lead");
Console.WriteLine($"     Persona: {lead.Member.Persona}");
Console.WriteLine();

// ── Step 3: Onboard agents ────────────────────────────────────────────────────

PrintStep("Step 3 — Onboard agents");

foreach (var (agentName, roleId, member) in members)
{
    var agentDir = Path.Combine(squadDir, "agents", agentName);
    Directory.CreateDirectory(agentDir);

    await File.WriteAllTextAsync(
        Path.Combine(agentDir, "CHARTER.md"),
        $"""
        # {member.Name}

        **Role**: {roleId}
        **Universe**: {member.Universe}
        **Persona**: {member.Persona}

        ## Traits
        {string.Join(Environment.NewLine, member.Traits.Select(t => $"- {t}"))}
        """);

    await File.WriteAllTextAsync(
        Path.Combine(agentDir, "HISTORY.md"),
        $"# History — {agentName}{Environment.NewLine}");

    Console.WriteLine($"  ✅ {agentName} — {roleId}");
}

Console.WriteLine();

// ── Step 4: Team roster ───────────────────────────────────────────────────────

PrintStep("Step 4 — Team roster");

Console.WriteLine($"  {"Name",-12} {"Role",-12} {"Universe",-20}");
Console.WriteLine($"  {new string('─', 46)}");
foreach (var (agentName, roleId, member) in members)
    Console.WriteLine($"  {agentName,-12} {roleId,-12} {member.Universe,-20}");

Console.WriteLine();

// ── Step 5: Casting history (persistent identities) ──────────────────────────

PrintStep("Step 5 — Casting history (persistent names)");

var engine2 = new CastingEngine(castingConfig, loggerFactory.CreateLogger<CastingEngine>());
foreach (var (agentName, roleId, _) in members)
    engine2.Cast(agentName, roleId, "The Usual Suspects");

var firstCast  = engine.GetAllCasts();
var secondCast = engine2.GetAllCasts();

var universesMatch = firstCast.Count == secondCast.Count
    && firstCast.Zip(secondCast).All(p => p.First.Member.Universe == p.Second.Member.Universe);

Console.WriteLine($"  Casting records: {firstCast.Count + secondCast.Count}");
Console.WriteLine($"  Names consistent across casts: {(universesMatch ? "✅ Yes" : "❌ No")}");
Console.WriteLine();

Directory.Delete(tempDir, recursive: true);

static void PrintStep(string title)
{
    Console.WriteLine(new string('─', 60));
    Console.WriteLine($"  {title}");
    Console.WriteLine(new string('─', 60));
}
