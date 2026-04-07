using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Squad.SDK.NET.Skills;

/// <summary>
/// Scans skill markdown content for high-confidence security patterns (Phase 1).
/// Pure static scanner — no I/O, no git operations.
/// </summary>
/// <remarks>
/// Ports the upstream <c>scanSkillContent()</c> function from
/// <c>scripts/security-review.mjs</c> (upstream dev@7479206).
///
/// Suppression rules (Phase 1):
/// <list type="bullet">
///   <item>Lines inside closed fenced code blocks are skipped.</item>
///   <item>Inline code spans (backtick pairs) are stripped before matching.</item>
///   <item>Markdown table rows documenting regex patterns are suppressed for credential checks.</item>
///   <item>Placeholder tokens (<c>sk-...</c>, <c>ghp_xxxx</c>, <c>AKIA...</c>) are ignored.</item>
///   <item>Fail-safe: unclosed fences disable all fence suppression (fail-open for security).</item>
/// </list>
/// </remarks>
public static class SkillSecurityScanner
{
    // ---------------------------------------------------------------------------
    // Pattern tables
    // ---------------------------------------------------------------------------

    private static readonly (string Name, Regex Pattern)[] CredentialPatterns =
    [
        ("AWS Access Key",        new Regex(@"AKIA[0-9A-Z]{16}",                                                       RegexOptions.Compiled)),
        ("GitHub PAT",            new Regex(@"ghp_[A-Za-z0-9]{36,}",                                                   RegexOptions.Compiled)),
        ("GitHub OAuth",          new Regex(@"gho_[A-Za-z0-9]{36,}",                                                   RegexOptions.Compiled)),
        ("GitHub App Token",      new Regex(@"ghu_[A-Za-z0-9]{36,}",                                                   RegexOptions.Compiled)),
        ("OpenAI Key",            new Regex(@"sk-[A-Za-z0-9]{20,}",                                                    RegexOptions.Compiled)),
        ("Private Key",           new Regex(@"-----BEGIN [A-Z ]*PRIVATE KEY-----",                                     RegexOptions.Compiled)),
        ("JWT Token",             new Regex(@"eyJ[A-Za-z0-9_-]{10,}\.eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]+",         RegexOptions.Compiled)),
        ("npm Token",             new Regex(@"npm_[A-Za-z0-9]{36,}",                                                   RegexOptions.Compiled)),
        ("Slack Token",           new Regex(@"xox[bpors]-[A-Za-z0-9-]+",                                               RegexOptions.Compiled)),
        ("Generic Secret Assign", new Regex(@"(?:API_KEY|SECRET|TOKEN|PASSWORD)\s*=\s*[""']?[A-Za-z0-9+/=_-]{20,}",   RegexOptions.Compiled)),
    ];

    private static readonly (string Name, Regex Pattern)[] CredentialFileReadPatterns =
    [
        (".env read (cmd)",        new Regex(@"(?:cat|type|Get-Content|less|more|head|tail)\s+.*\.env(?!\.example|\.sample|\.template)\b", RegexOptions.Compiled)),
        (".env read (js)",         new Regex(@"(?:readFileSync|readFile)\s*\(.*\.env(?!\.example|\.sample|\.template)\b",                   RegexOptions.Compiled)),
        ("Private key read (cmd)", new Regex(@"(?:cat|type|Get-Content)\s+.*(?:id_rsa|id_ed25519|\.pem|\.key)\b",                           RegexOptions.Compiled)),
        ("Private key read (js)",  new Regex(@"(?:readFileSync|readFile)\s*\(.*(?:id_rsa|id_ed25519|\.pem|\.key)\b",                        RegexOptions.Compiled)),
        ("AWS credentials read",   new Regex(@"(?:cat|type|readFileSync|Get-Content)\s*[\s(].*\.aws/credentials",                           RegexOptions.Compiled)),
        (".npmrc read",            new Regex(@"(?:cat|type|readFileSync|Get-Content)\s*[\s(].*\.npmrc",                                     RegexOptions.Compiled)),
        (".netrc read",            new Regex(@"(?:cat|type|readFileSync|Get-Content)\s*[\s(].*\.netrc",                                     RegexOptions.Compiled)),
    ];

    private static readonly (string Name, Regex Pattern)[] DownloadExecPatterns =
    [
        ("curl pipe bash",    new Regex(@"curl\s+.*\|\s*(?:bash|sh|zsh)",                                RegexOptions.Compiled)),
        ("wget pipe bash",    new Regex(@"wget\s+.*\|\s*(?:bash|sh|zsh)",                                RegexOptions.Compiled)),
        ("irm pipe iex",      new Regex(@"irm\s+.*\|\s*iex",                                            RegexOptions.Compiled)),
        ("Invoke-Expression", new Regex(@"Invoke-Expression\s+.*(?:http|ftp|Invoke-WebRequest|irm)",    RegexOptions.Compiled)),
        ("powershell -enc",   new Regex(@"powershell\s+.*-[Ee]nc(?:oded)?[Cc]ommand",                   RegexOptions.Compiled)),
        ("eval curl",         new Regex(@"eval\s+""\$\(curl",                                           RegexOptions.Compiled)),
        ("source curl",       new Regex(@"source\s+<\(curl",                                            RegexOptions.Compiled)),
        ("bash process sub",  new Regex(@"bash\s+<\(curl",                                              RegexOptions.Compiled)),
    ];

    private static readonly (string Name, Regex Pattern)[] PrivEscPatterns =
    [
        ("sudo bash/sh",       new Regex(@"sudo\s+(?:bash|sh|zsh|su)",                    RegexOptions.Compiled)),
        ("sudo rm",            new Regex(@"sudo\s+rm\b",                                  RegexOptions.Compiled)),
        ("RunAs admin",        new Regex(@"Start-Process\s+.*-Verb\s+RunAs",              RegexOptions.Compiled)),
        ("SetExecutionPolicy", new Regex(@"Set-ExecutionPolicy\s+(?:Bypass|Unrestricted)", RegexOptions.Compiled)),
        ("chmod 777",          new Regex(@"chmod\s+777",                                  RegexOptions.Compiled)),
    ];

    // ---------------------------------------------------------------------------
    // Suppression helpers
    // ---------------------------------------------------------------------------

    /// <summary>Placeholder marker regex: ellipsis, repeated x/X/_, or angle-bracket token.</summary>
    private static readonly Regex PlaceholderRe = new(@"\.{2,}|x{4,}|X{4,}|_{4,}|<[^>]+>", RegexOptions.Compiled);

    /// <summary>Regex syntax markers: character classes <c>[..]</c> or quantifiers <c>{n,m}</c>.</summary>
    private static readonly Regex RegexSyntaxRe = new(@"\[[^\]]+\]|\{\d+[,\d]*\}", RegexOptions.Compiled);

    /// <summary>Inline code span detector (backtick pair).</summary>
    private static readonly Regex InlineCodeRe = new(@"`[^`]*`", RegexOptions.Compiled);

    /// <summary>Fenced code block opening delimiter (CommonMark: 0–3 leading spaces, 3+ backticks or tildes).</summary>
    private static readonly Regex FenceOpenRe = new(@"^\s{0,3}((`{3,})|(~{3,}))", RegexOptions.Compiled);

    /// <summary>Markdown table row start (leading <c>|</c>).</summary>
    private static readonly Regex TableRowLeadRe = new(@"^\s*\|", RegexOptions.Compiled);

    /// <summary>Cache of compiled close-fence regexes keyed by (fenceChar, fenceLen).</summary>
    private static readonly ConcurrentDictionary<(char, int), Regex> CloseReCache = new();

    // ---------------------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Scans skill markdown content for high-confidence security patterns.
    /// </summary>
    /// <param name="content">Full markdown file content.</param>
    /// <param name="filePath">Repository-relative file path (included in findings).</param>
    /// <returns>
    /// A read-only list of <see cref="SkillSecurityFinding"/> instances,
    /// or an empty list when no issues are found.
    /// </returns>
    public static IReadOnlyList<SkillSecurityFinding> ScanContent(string content, string filePath)
    {
        var findings = new List<SkillSecurityFinding>();
        var lines = content.Split('\n');
        var (fenced, unclosed) = ParseFencedRegions(lines);
        var suppressFenced = !unclosed;

        for (var i = 0; i < lines.Length; i++)
        {
            if (suppressFenced && fenced.Contains(i))
                continue;

            // Strip inline code spans before matching
            var scanText = InlineCodeRe.Replace(lines[i], string.Empty);
            var isRegexDocRow = TableRowLeadRe.IsMatch(lines[i]) && RegexSyntaxRe.IsMatch(lines[i]);

            // P1: Embedded credentials (suppressed on regex-doc table rows)
            if (!isRegexDocRow)
            {
                foreach (var (name, regex) in CredentialPatterns)
                {
                    var m = regex.Match(scanText);
                    if (m.Success && !PlaceholderRe.IsMatch(m.Value))
                    {
                        findings.Add(new SkillSecurityFinding
                        {
                            Category = "skill-credentials",
                            Severity = "error",
                            Message  = $"Possible embedded credential ({name}) in skill file.",
                            File     = filePath,
                            Line     = i + 1,
                        });
                    }
                }
            }

            // P2: Credential file reads
            foreach (var (name, regex) in CredentialFileReadPatterns)
            {
                if (regex.IsMatch(scanText))
                {
                    findings.Add(new SkillSecurityFinding
                    {
                        Category = "skill-credential-file-read",
                        Severity = "error",
                        Message  = $"Credential file read instruction ({name}) in skill file.",
                        File     = filePath,
                        Line     = i + 1,
                    });
                }
            }

            // P3: Download-and-execute
            foreach (var (name, regex) in DownloadExecPatterns)
            {
                if (regex.IsMatch(scanText))
                {
                    findings.Add(new SkillSecurityFinding
                    {
                        Category = "skill-download-exec",
                        Severity = "error",
                        Message  = $"Download-and-execute pattern ({name}) in skill file.",
                        File     = filePath,
                        Line     = i + 1,
                    });
                }
            }

            // P4: Privilege escalation
            foreach (var (name, regex) in PrivEscPatterns)
            {
                if (regex.IsMatch(scanText))
                {
                    findings.Add(new SkillSecurityFinding
                    {
                        Category = "skill-privilege-escalation",
                        Severity = "error",
                        Message  = $"Privilege escalation pattern ({name}) in skill file.",
                        File     = filePath,
                        Line     = i + 1,
                    });
                }
            }
        }

        return findings.AsReadOnly();
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Detects fenced code block regions in markdown (CommonMark-compliant).
    /// Handles backtick and tilde fences with variable-length delimiters and up to 3 leading spaces.
    /// </summary>
    /// <returns>
    /// The set of 0-based line indices inside a fence, and whether any fence is unclosed.
    /// An unclosed fence means the fail-safe fires: all fence suppression is disabled.
    /// </returns>
    private static (HashSet<int> Fenced, bool Unclosed) ParseFencedRegions(string[] lines)
    {
        var inFence  = false;
        var fenceLen = 0;
        Regex? closeRe = null;
        var fenced = new HashSet<int>();

        for (var i = 0; i < lines.Length; i++)
        {
            if (!inFence)
            {
                var m = FenceOpenRe.Match(lines[i]);
                if (m.Success)
                {
                    inFence = true;
                    var fenceChar = m.Groups[2].Success ? '`' : '~';
                    fenceLen = (m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Value).Length;
                    closeRe = CloseReCache.GetOrAdd((fenceChar, fenceLen), static key =>
                    {
                        var closeChar = Regex.Escape(key.Item1.ToString());
                        return new Regex($@"^\s{{0,3}}{closeChar}{{{key.Item2},}}\s*$", RegexOptions.Compiled);
                    });
                    fenced.Add(i);
                }
            }
            else
            {
                // Add the line to fenced set before checking whether it closes the fence
                fenced.Add(i);
                if (closeRe!.IsMatch(lines[i]))
                {
                    inFence = false;
                    closeRe = null;
                }
            }
        }

        return (fenced, inFence);
    }
}
