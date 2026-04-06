using System.Text.RegularExpressions;

namespace Squad.SDK.NET.Security;

/// <summary>
/// Severity level for a <see cref="SkillSecurityFinding"/>.
/// </summary>
public enum SkillFindingSeverity
{
    /// <summary>Informational only.</summary>
    Info,
    /// <summary>Potential issue that warrants attention.</summary>
    Warning,
    /// <summary>High-confidence security concern that should be addressed.</summary>
    Error,
}

/// <summary>
/// A single security finding produced by <see cref="SkillSecurityScanner"/>.
/// </summary>
public sealed record SkillSecurityFinding
{
    /// <summary>Gets the category of the finding (e.g. <c>skill-credentials</c>).</summary>
    public required string Category { get; init; }
    /// <summary>Gets the severity of the finding.</summary>
    public required SkillFindingSeverity Severity { get; init; }
    /// <summary>Gets a human-readable description of the finding.</summary>
    public required string Message { get; init; }
    /// <summary>Gets the repo-relative path of the file where the finding was detected.</summary>
    public required string File { get; init; }
    /// <summary>Gets the 1-based line number where the finding was detected.</summary>
    public required int Line { get; init; }
}

/// <summary>
/// Markdown-aware security scanner for Squad skill files
/// (<c>.copilot/skills/**/*.md</c> and <c>.squad/skills/**/*.md</c>).
///
/// Phase 1 pattern detection:
/// <list type="bullet">
///   <item><description>P1 — Embedded credential patterns (AWS, GitHub, OpenAI, JWT, etc.)</description></item>
///   <item><description>P2 — Credential file read patterns (<c>.env</c>, <c>.ssh/id_rsa</c>, <c>.aws/credentials</c>, <c>.npmrc</c>, <c>.netrc</c>)</description></item>
///   <item><description>P3 — Download-and-execute patterns (<c>curl|bash</c>, <c>irm|iex</c>, etc.)</description></item>
///   <item><description>P4 — Privilege escalation patterns (<c>sudo</c>, <c>RunAs</c>, <c>chmod 777</c>, etc.)</description></item>
/// </list>
///
/// Suppression (Phase 1):
/// <list type="bullet">
///   <item><description>Lines inside fenced code blocks are skipped (CommonMark-compliant: backtick/tilde, variable length).</description></item>
///   <item><description>Inline code spans (backtick pairs) are stripped before matching.</description></item>
///   <item><description>Markdown table rows documenting regex patterns are suppressed for credential matches.</description></item>
///   <item><description>Placeholder tokens (<c>sk-...</c>, <c>ghp_xxxx</c>, <c>AKIA...</c>, angle brackets) are ignored.</description></item>
///   <item><description>Fail-safe: unclosed fences = UNSUPPRESSED (fail-open for security).</description></item>
/// </list>
/// </summary>
public static partial class SkillSecurityScanner
{
    // -------------------------------------------------------------------------
    // P1: Embedded credential patterns
    // -------------------------------------------------------------------------

    private static readonly (string Name, Regex Pattern)[] CredentialPatterns =
    [
        ("AWS Access Key",        AwsAccessKeyRegex()),
        ("GitHub PAT",            GithubPatRegex()),
        ("GitHub OAuth",          GithubOAuthRegex()),
        ("GitHub App Token",      GithubAppTokenRegex()),
        ("OpenAI Key",            OpenAiKeyRegex()),
        ("Private Key",           PrivateKeyRegex()),
        ("JWT Token",             JwtTokenRegex()),
        ("npm Token",             NpmTokenRegex()),
        ("Slack Token",           SlackTokenRegex()),
        ("Generic Secret Assign", GenericSecretAssignRegex()),
    ];

    // -------------------------------------------------------------------------
    // P2: Credential file read patterns
    // -------------------------------------------------------------------------

    private static readonly (string Name, Regex Pattern)[] CredFileReadPatterns =
    [
        (".env read (cmd)",       EnvReadCmdRegex()),
        (".env read (code)",      EnvReadCodeRegex()),
        ("Private key read (cmd)",PrivKeyReadCmdRegex()),
        ("Private key read (code)",PrivKeyReadCodeRegex()),
        ("AWS credentials read",  AwsCredReadRegex()),
        (".npmrc read",           NpmrcReadRegex()),
        (".netrc read",           NetrcReadRegex()),
    ];

    // -------------------------------------------------------------------------
    // P3: Download-and-execute patterns
    // -------------------------------------------------------------------------

    private static readonly (string Name, Regex Pattern)[] DownloadExecPatterns =
    [
        ("curl pipe bash",        CurlPipeBashRegex()),
        ("wget pipe bash",        WgetPipeBashRegex()),
        ("irm pipe iex",          IrmPipeIexRegex()),
        ("Invoke-Expression",     InvokeExpressionRegex()),
        ("powershell -enc",       PwshEncRegex()),
        ("eval curl",             EvalCurlRegex()),
        ("source curl",           SourceCurlRegex()),
        ("bash process sub",      BashProcSubRegex()),
    ];

    // -------------------------------------------------------------------------
    // P4: Privilege escalation patterns
    // -------------------------------------------------------------------------

    private static readonly (string Name, Regex Pattern)[] PrivEscPatterns =
    [
        ("sudo bash/sh",          SudoBashRegex()),
        ("sudo rm",               SudoRmRegex()),
        ("RunAs admin",           RunAsAdminRegex()),
        ("SetExecutionPolicy",    SetExecPolicyRegex()),
        ("chmod 777",             Chmod777Regex()),
    ];

    // -------------------------------------------------------------------------
    // Placeholder / regex-doc detection
    // -------------------------------------------------------------------------

    [GeneratedRegex(@"\.{2,}|x{4,}|X{4,}|_{4,}|<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();

    [GeneratedRegex(@"\[[^\]]+\]|\{\d+[,\d]*\}", RegexOptions.Compiled)]
    private static partial Regex RegexSyntaxRegex();

    [GeneratedRegex(@"`[^`]*`", RegexOptions.Compiled)]
    private static partial Regex InlineCodeRegex();

    // Fenced code block opening: 0-3 leading spaces + 3+ backticks or tildes
    [GeneratedRegex(@"^\s{0,3}(`{3,}|~{3,})", RegexOptions.Compiled)]
    private static partial Regex FenceOpenRegex();

    // -------------------------------------------------------------------------
    // Credential patterns (generated regexes)
    // -------------------------------------------------------------------------

    [GeneratedRegex(@"AKIA[0-9A-Z]{16}", RegexOptions.Compiled)]
    private static partial Regex AwsAccessKeyRegex();

    [GeneratedRegex(@"ghp_[A-Za-z0-9]{36,}", RegexOptions.Compiled)]
    private static partial Regex GithubPatRegex();

    [GeneratedRegex(@"gho_[A-Za-z0-9]{36,}", RegexOptions.Compiled)]
    private static partial Regex GithubOAuthRegex();

    [GeneratedRegex(@"ghu_[A-Za-z0-9]{36,}", RegexOptions.Compiled)]
    private static partial Regex GithubAppTokenRegex();

    [GeneratedRegex(@"sk-[A-Za-z0-9]{20,}", RegexOptions.Compiled)]
    private static partial Regex OpenAiKeyRegex();

    [GeneratedRegex(@"-----BEGIN [A-Z ]*PRIVATE KEY-----", RegexOptions.Compiled)]
    private static partial Regex PrivateKeyRegex();

    [GeneratedRegex(@"eyJ[A-Za-z0-9_\-]{10,}\.eyJ[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]+", RegexOptions.Compiled)]
    private static partial Regex JwtTokenRegex();

    [GeneratedRegex(@"npm_[A-Za-z0-9]{36,}", RegexOptions.Compiled)]
    private static partial Regex NpmTokenRegex();

    [GeneratedRegex(@"xox[bpors]-[A-Za-z0-9\-]+", RegexOptions.Compiled)]
    private static partial Regex SlackTokenRegex();

    [GeneratedRegex(@"(?:API_KEY|SECRET|TOKEN|PASSWORD)\s*=\s*[""']?[A-Za-z0-9+/=_\-]{20,}", RegexOptions.Compiled)]
    private static partial Regex GenericSecretAssignRegex();

    // -------------------------------------------------------------------------
    // Credential file read patterns (generated regexes)
    // -------------------------------------------------------------------------

    [GeneratedRegex(@"(?:cat|type|Get-Content|less|more|head|tail)\s+.*\.env(?!\.example|\.sample|\.template)\b", RegexOptions.Compiled)]
    private static partial Regex EnvReadCmdRegex();

    [GeneratedRegex(@"(?:readFileSync|readFile|File\.ReadAllText|File\.ReadAllLines)\s*\(.*\.env(?!\.example|\.sample|\.template)\b", RegexOptions.Compiled)]
    private static partial Regex EnvReadCodeRegex();

    [GeneratedRegex(@"(?:cat|type|Get-Content)\s+.*(?:id_rsa|id_ed25519|\.pem|\.key)\b", RegexOptions.Compiled)]
    private static partial Regex PrivKeyReadCmdRegex();

    [GeneratedRegex(@"(?:readFileSync|readFile|File\.ReadAllText)\s*\(.*(?:id_rsa|id_ed25519|\.pem|\.key)\b", RegexOptions.Compiled)]
    private static partial Regex PrivKeyReadCodeRegex();

    [GeneratedRegex(@"(?:cat|type|readFileSync|Get-Content)\s*[\s(].*\.aws/credentials", RegexOptions.Compiled)]
    private static partial Regex AwsCredReadRegex();

    [GeneratedRegex(@"(?:cat|type|readFileSync|Get-Content)\s*[\s(].*\.npmrc", RegexOptions.Compiled)]
    private static partial Regex NpmrcReadRegex();

    [GeneratedRegex(@"(?:cat|type|readFileSync|Get-Content)\s*[\s(].*\.netrc", RegexOptions.Compiled)]
    private static partial Regex NetrcReadRegex();

    // -------------------------------------------------------------------------
    // Download-and-execute patterns (generated regexes)
    // -------------------------------------------------------------------------

    [GeneratedRegex(@"curl\s+.*\|\s*(?:bash|sh|zsh)", RegexOptions.Compiled)]
    private static partial Regex CurlPipeBashRegex();

    [GeneratedRegex(@"wget\s+.*\|\s*(?:bash|sh|zsh)", RegexOptions.Compiled)]
    private static partial Regex WgetPipeBashRegex();

    [GeneratedRegex(@"irm\s+.*\|\s*iex", RegexOptions.Compiled)]
    private static partial Regex IrmPipeIexRegex();

    [GeneratedRegex(@"Invoke-Expression\s+.*(?:http|ftp|Invoke-WebRequest|irm)", RegexOptions.Compiled)]
    private static partial Regex InvokeExpressionRegex();

    [GeneratedRegex(@"powershell\s+.*-[Ee]nc(?:oded)?[Cc]ommand", RegexOptions.Compiled)]
    private static partial Regex PwshEncRegex();

    [GeneratedRegex(@"eval\s+""\$\(curl", RegexOptions.Compiled)]
    private static partial Regex EvalCurlRegex();

    [GeneratedRegex(@"source\s+<\(curl", RegexOptions.Compiled)]
    private static partial Regex SourceCurlRegex();

    [GeneratedRegex(@"bash\s+<\(curl", RegexOptions.Compiled)]
    private static partial Regex BashProcSubRegex();

    // -------------------------------------------------------------------------
    // Privilege escalation patterns (generated regexes)
    // -------------------------------------------------------------------------

    [GeneratedRegex(@"sudo\s+(?:bash|sh|zsh|su)\b", RegexOptions.Compiled)]
    private static partial Regex SudoBashRegex();

    [GeneratedRegex(@"sudo\s+rm\b", RegexOptions.Compiled)]
    private static partial Regex SudoRmRegex();

    [GeneratedRegex(@"Start-Process\s+.*-Verb\s+RunAs", RegexOptions.Compiled)]
    private static partial Regex RunAsAdminRegex();

    [GeneratedRegex(@"Set-ExecutionPolicy\s+(?:Bypass|Unrestricted)", RegexOptions.Compiled)]
    private static partial Regex SetExecPolicyRegex();

    [GeneratedRegex(@"chmod\s+777", RegexOptions.Compiled)]
    private static partial Regex Chmod777Regex();

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Determines whether the given file path is a skill file that should be scanned.
    /// </summary>
    /// <param name="filePath">Repo-relative or absolute file path.</param>
    /// <returns><see langword="true"/> when the path matches <c>.copilot/skills/**/*.md</c> or <c>.squad/skills/**/*.md</c>.</returns>
    public static bool IsSkillFile(string filePath)
    {
        var normalized = filePath.Replace('\\', '/');
        return (normalized.Contains(".copilot/skills/") || normalized.Contains(".squad/skills/"))
               && normalized.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Scans the content of a skill markdown file for high-confidence security patterns.
    /// This is a pure function with no side effects or I/O.
    /// </summary>
    /// <param name="content">Full markdown file content.</param>
    /// <param name="filePath">Repo-relative file path used to populate findings.</param>
    /// <returns>A list of <see cref="SkillSecurityFinding"/> instances (empty when no issues found).</returns>
    public static IReadOnlyList<SkillSecurityFinding> ScanSkillContent(string content, string filePath)
    {
        var findings = new List<SkillSecurityFinding>();
        var lines = content.Split('\n');
        var (fenced, unclosed) = ParseFencedRegions(lines);
        var suppressFenced = !unclosed;

        for (var i = 0; i < lines.Length; i++)
        {
            if (suppressFenced && fenced.Contains(i)) continue;

            var scanText = StripInlineCode(lines[i]);
            var isRegexDocRow = IsRegexDocRow(lines[i]);

            // P1: Embedded credentials (suppress on regex-doc table rows)
            if (!isRegexDocRow)
            {
                foreach (var (name, pattern) in CredentialPatterns)
                {
                    var m = pattern.Match(scanText);
                    if (m.Success && !IsPlaceholder(m.Value))
                    {
                        findings.Add(new SkillSecurityFinding
                        {
                            Category = "skill-credentials",
                            Severity = SkillFindingSeverity.Error,
                            Message = $"Possible embedded credential ({name}) in skill file.",
                            File = filePath,
                            Line = i + 1,
                        });
                    }
                }
            }

            // P2: Credential file reads
            foreach (var (name, pattern) in CredFileReadPatterns)
            {
                if (pattern.IsMatch(scanText))
                {
                    findings.Add(new SkillSecurityFinding
                    {
                        Category = "skill-credential-file-read",
                        Severity = SkillFindingSeverity.Error,
                        Message = $"Credential file read instruction ({name}) in skill file.",
                        File = filePath,
                        Line = i + 1,
                    });
                }
            }

            // P3: Download-and-execute
            foreach (var (name, pattern) in DownloadExecPatterns)
            {
                if (pattern.IsMatch(scanText))
                {
                    findings.Add(new SkillSecurityFinding
                    {
                        Category = "skill-download-exec",
                        Severity = SkillFindingSeverity.Error,
                        Message = $"Download-and-execute pattern ({name}) in skill file.",
                        File = filePath,
                        Line = i + 1,
                    });
                }
            }

            // P4: Privilege escalation
            foreach (var (name, pattern) in PrivEscPatterns)
            {
                if (pattern.IsMatch(scanText))
                {
                    findings.Add(new SkillSecurityFinding
                    {
                        Category = "skill-privilege-escalation",
                        Severity = SkillFindingSeverity.Error,
                        Message = $"Privilege escalation pattern ({name}) in skill file.",
                        File = filePath,
                        Line = i + 1,
                    });
                }
            }
        }

        return findings;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Detects fenced code block regions in markdown.
    /// Handles backtick and tilde fences, variable-length delimiters,
    /// and up to 3 leading spaces per CommonMark.
    /// </summary>
    internal static (HashSet<int> Fenced, bool Unclosed) ParseFencedRegions(string[] lines)
    {
        var inFence = false;
        var fenceChar = ' ';
        var fenceLen = 0;
        var fenced = new HashSet<int>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (!inFence)
            {
                var m = FenceOpenRegex().Match(line);
                if (m.Success)
                {
                    inFence = true;
                    fenceChar = m.Groups[1].Value[0];
                    fenceLen = m.Groups[1].Length;
                    fenced.Add(i);
                }
            }
            else
            {
                fenced.Add(i);
                // Closing fence: same char, same or longer run, optional trailing spaces
                var closePattern = new string(fenceChar, fenceLen);
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith(closePattern, StringComparison.Ordinal)
                    && trimmed.TrimStart(fenceChar).Trim().Length == 0)
                {
                    inFence = false;
                }
            }
        }

        return (fenced, inFence);
    }

    /// <summary>Removes inline code spans (backtick-delimited) from a line.</summary>
    internal static string StripInlineCode(string line)
        => InlineCodeRegex().Replace(line, "");

    /// <summary>Checks whether a regex match looks like a placeholder token.</summary>
    internal static bool IsPlaceholder(string matched)
        => PlaceholderRegex().IsMatch(matched);

    /// <summary>
    /// Checks whether a line is a markdown table row documenting regex patterns.
    /// Used to suppress credential findings on pattern-documentation tables.
    /// </summary>
    internal static bool IsRegexDocRow(string line)
        => line.TrimStart().StartsWith('|') && RegexSyntaxRegex().IsMatch(line);
}
