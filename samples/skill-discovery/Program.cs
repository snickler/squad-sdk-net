using Squad.SDK.NET.Skills;

Console.WriteLine("Squad SDK - Skill Discovery Demo (.NET)");
Console.WriteLine();

var tempDir   = Path.Combine(Path.GetTempPath(), $"squad-skills-demo-{Guid.NewGuid():N}");
var skillsDir = Path.Combine(tempDir, ".squad", "skills");
Directory.CreateDirectory(skillsDir);

Console.WriteLine("  Creating temporary skill files...");
Console.WriteLine($"  Skills directory: {skillsDir}");

await WriteSkill(skillsDir, "typescript-patterns.md", """
    ---
    id: typescript-patterns
    name: TypeScript Patterns
    triggers: [typescript, types, generics, inference, strict]
    agentRoles: [developer, lead]
    confidence: low
    ---
    ## TypeScript Patterns
    Prefer `unknown` over `any` for type-safe narrowing.
    """);

await WriteSkill(skillsDir, "architecture-patterns.md", """
    ---
    id: architecture-patterns
    name: Architecture Patterns
    triggers: [architecture, design, scalability, microservices, patterns]
    agentRoles: [lead, architect]
    confidence: medium
    ---
    ## Architecture Patterns
    Prefer vertical slicing over horizontal layering.
    """);

await WriteSkill(skillsDir, "quality-practices.md", """
    ---
    id: quality-practices
    name: Quality Practices
    triggers: [test, testing, coverage, quality, review, lint]
    agentRoles: [tester, developer]
    confidence: high
    ---
    ## Quality Practices
    Write tests that describe behaviour, not implementation.
    """);

Console.WriteLine("  Created 3 skill files");
Console.WriteLine();

// Step 1: Load skills
PrintStep("Step 1: Load Skills from Directory");
var skills = await SkillLoader.LoadDirectoryAsync(skillsDir);
foreach (var skill in skills)
{
    var icon = skill.Confidence switch
    {
        SkillConfidence.Low    => "[LOW]",
        SkillConfidence.Medium => "[MED]",
        SkillConfidence.High   => "[HIGH]",
        _                      => "[ ? ]"
    };
    Console.WriteLine($"  {icon} {skill.Name}");
    Console.WriteLine($"       ID: {skill.Id}  Triggers: [{string.Join(", ", skill.Triggers)}]");
    Console.WriteLine();
}

// Step 2: Register skills
PrintStep("Step 2: Register Skills in SkillRegistry");
var registry = new SkillRegistry();
foreach (var skill in skills)
    registry.Register(skill);
Console.WriteLine($"  Registered {registry.GetAll().Count} skills");
Console.WriteLine();

// Step 3: Match skills to tasks
PrintStep("Step 3: Match Skills to Tasks");

(string Task, string Role)[] queries =
[
    ("Add TypeScript generics to the data layer",         "developer"),
    ("Design microservices for the payment system",       "lead"),
    ("Improve test coverage for the authentication flow", "tester"),
];

foreach (var (task, role) in queries)
{
    Console.WriteLine($"  Task: \"{task}\"  (role: {role})");
    var matches = registry.Match(task, role);
    if (matches.Count == 0)
        Console.WriteLine("     -> (no matching skills)");
    else
        foreach (var m in matches)
            Console.WriteLine($"     -> {m.Skill.Name} (score: {(int)(m.Score * 100)}%) -- {m.Reason}");
    Console.WriteLine();
}

// Step 4: Runtime skill discovery
PrintStep("Step 4: Agent Discovers a New Pattern");
Console.WriteLine("  Agent detected pattern: 'Always use Result<T, E> for error handling'");
Console.WriteLine();

var newSkillContent = """
    ---
    id: error-handling-patterns
    name: Error Handling Patterns
    triggers: [error, exception, result, failure, handling]
    agentRoles: [developer, tester]
    confidence: low
    ---
    ## Error Handling Patterns
    Prefer Result<T, E> over exceptions for expected failure paths.
    """;

var tempPath = Path.Combine(skillsDir, "error-handling-patterns.md");
await File.WriteAllTextAsync(tempPath, newSkillContent.Trim());
var discovered = await SkillLoader.LoadAsync(tempPath);
registry.Register(discovered);
Console.WriteLine($"  New skill registered: \"{discovered.Name}\"");

var errorMatches = registry.Match("handle error result failure");
foreach (var m in errorMatches)
    Console.WriteLine($"     -> {m.Skill.Name} (score: {(int)(m.Score * 100)}%)");
Console.WriteLine();

// Step 5: Confidence lifecycle
PrintStep("Step 5: Confidence Lifecycle");
Console.WriteLine("  [LOW]  -- First observation; pattern just noticed");
Console.WriteLine("  [MED]  -- Confirmed; validated across sessions");
Console.WriteLine("  [HIGH] -- Established; proven team standard");
Console.WriteLine();

Directory.Delete(tempDir, recursive: true);

static async Task WriteSkill(string dir, string filename, string content) =>
    await File.WriteAllTextAsync(Path.Combine(dir, filename), content.Trim());

static void PrintStep(string title)
{
    Console.WriteLine($"  -- {title} --");
    Console.WriteLine();
}
