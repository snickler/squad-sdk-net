using Squad.SDK.NET.Platform;
using Squad.SDK.NET.Resolution;

namespace Squad.SDK.NET.Tests;

#region SquadResolver — ScratchDir

public sealed class ScratchDirTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"squad-scratch-test-{Guid.NewGuid():N}");
    private readonly string _squadRoot;

    public ScratchDirTests()
    {
        _squadRoot = Path.Combine(_testRoot, ".squad");
        Directory.CreateDirectory(_squadRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
            Directory.Delete(_testRoot, recursive: true);
    }

    [Fact]
    public void ScratchDir_DefaultCreate_CreatesDirectory()
    {
        var dir = SquadResolver.ScratchDir(_squadRoot);

        Assert.Equal(Path.Combine(_squadRoot, ".scratch"), dir);
        Assert.True(Directory.Exists(dir));
    }

    [Fact]
    public void ScratchDir_CreateFalse_ReturnsPathWithoutCreating()
    {
        var dir = SquadResolver.ScratchDir(_squadRoot, create: false);

        Assert.Equal(Path.Combine(_squadRoot, ".scratch"), dir);
        Assert.False(Directory.Exists(dir));
    }

    [Fact]
    public void ScratchDir_CalledTwice_IsIdempotent()
    {
        SquadResolver.ScratchDir(_squadRoot);
        SquadResolver.ScratchDir(_squadRoot); // should not throw

        Assert.True(Directory.Exists(Path.Combine(_squadRoot, ".scratch")));
    }
}

#endregion

#region SquadResolver — ScratchFile

public sealed class ScratchFileTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"squad-scratchfile-test-{Guid.NewGuid():N}");
    private readonly string _squadRoot;

    public ScratchFileTests()
    {
        _squadRoot = Path.Combine(_testRoot, ".squad");
        Directory.CreateDirectory(_squadRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
            Directory.Delete(_testRoot, recursive: true);
    }

    [Fact]
    public void ScratchFile_DefaultExtension_ReturnsPathInsideScratchDir()
    {
        var filePath = SquadResolver.ScratchFile(_squadRoot, "test-prompt");

        Assert.Contains(".scratch", filePath);
        Assert.EndsWith(".tmp", filePath, StringComparison.OrdinalIgnoreCase);
        // Scratch directory is created automatically
        Assert.True(Directory.Exists(Path.GetDirectoryName(filePath)));
    }

    [Fact]
    public void ScratchFile_WithContent_CreatesFileWithContent()
    {
        var content = "hello from scratch";
        var filePath = SquadResolver.ScratchFile(_squadRoot, "msg", ".txt", content);

        Assert.True(File.Exists(filePath));
        Assert.Equal(content, File.ReadAllText(filePath));
    }

    [Fact]
    public void ScratchFile_CustomExtension_UsesExtension()
    {
        var filePath = SquadResolver.ScratchFile(_squadRoot, "fleet", ".md");

        Assert.EndsWith(".md", filePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScratchFile_SuccessiveCalls_GenerateUniqueFilenames()
    {
        var a = SquadResolver.ScratchFile(_squadRoot, "dup", ".txt", "a");
        var b = SquadResolver.ScratchFile(_squadRoot, "dup", ".txt", "b");

        Assert.NotEqual(a, b);
        Assert.True(File.Exists(a));
        Assert.True(File.Exists(b));
    }

    [Fact]
    public void ScratchFile_NoContent_DoesNotCreateFile()
    {
        var filePath = SquadResolver.ScratchFile(_squadRoot, "no-content", ".txt");

        // Directory is created but the file itself is not
        Assert.False(File.Exists(filePath));
        Assert.True(Directory.Exists(Path.GetDirectoryName(filePath)));
    }

    [Fact]
    public void ScratchFile_AutoCreatesScratchDirectory()
    {
        Assert.False(Directory.Exists(Path.Combine(_squadRoot, ".scratch")));

        SquadResolver.ScratchFile(_squadRoot, "auto-create", ".txt", "data");

        Assert.True(Directory.Exists(Path.Combine(_squadRoot, ".scratch")));
    }
}

#endregion

#region SquadResolver — Path Validation Helpers

public sealed class EnsureSquadPathTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"squad-path-test-{Guid.NewGuid():N}");
    private readonly string _squadRoot;

    public EnsureSquadPathTests()
    {
        _squadRoot = Path.Combine(_testRoot, ".squad");
        Directory.CreateDirectory(_squadRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
            Directory.Delete(_testRoot, recursive: true);
    }

    [Fact]
    public void EnsureSquadPath_InsideSquadRoot_ReturnsResolvedPath()
    {
        var filePath = Path.Combine(_squadRoot, "some-file.txt");
        var result = SquadResolver.EnsureSquadPath(filePath, _squadRoot);

        Assert.Equal(Path.GetFullPath(filePath), result);
    }

    [Fact]
    public void EnsureSquadPath_InsideTempDir_ReturnsResolvedPath()
    {
        var filePath = Path.Combine(Path.GetTempPath(), "some-file.txt");
        var result = SquadResolver.EnsureSquadPath(filePath, _squadRoot);

        Assert.Equal(Path.GetFullPath(filePath), result);
    }

    [Fact]
    public void EnsureSquadPath_OutsideSquadRoot_ThrowsArgumentException()
    {
        // Use a path outside both the squad root AND the system temp directory
        var outsidePath = OperatingSystem.IsWindows()
            ? @"C:\Windows\System32\outside.txt"
            : "/usr/local/outside-test.txt";

        Assert.Throws<ArgumentException>(() => SquadResolver.EnsureSquadPath(outsidePath, _squadRoot));
    }

    [Fact]
    public void EnsureSquadPathDual_InsideProjectDir_ReturnsResolvedPath()
    {
        var teamDir = Path.Combine(_testRoot, "team");
        Directory.CreateDirectory(teamDir);

        var filePath = Path.Combine(_squadRoot, "decisions", "adr.md");
        var result = SquadResolver.EnsureSquadPathDual(filePath, _squadRoot, teamDir);

        Assert.Equal(Path.GetFullPath(filePath), result);
    }

    [Fact]
    public void EnsureSquadPathDual_InsideTeamDir_ReturnsResolvedPath()
    {
        var teamDir = Path.Combine(_testRoot, "team");
        Directory.CreateDirectory(teamDir);

        var filePath = Path.Combine(teamDir, "agents.json");
        var result = SquadResolver.EnsureSquadPathDual(filePath, _squadRoot, teamDir);

        Assert.Equal(Path.GetFullPath(filePath), result);
    }

    [Fact]
    public void EnsureSquadPathDual_OutsideBothRoots_ThrowsArgumentException()
    {
        var teamDir = Path.Combine(_testRoot, "team");
        Directory.CreateDirectory(teamDir);

        // Use a path outside both squad roots AND the system temp directory
        var outsidePath = OperatingSystem.IsWindows()
            ? @"C:\Windows\System32\outside.txt"
            : "/usr/local/outside-test.txt";

        Assert.Throws<ArgumentException>(() =>
            SquadResolver.EnsureSquadPathDual(outsidePath, _squadRoot, teamDir));
    }

    [Fact]
    public void EnsureSquadPathTriple_InsidePersonalDir_ReturnsResolvedPath()
    {
        var teamDir = Path.Combine(_testRoot, "team");
        var personalDir = Path.Combine(_testRoot, "personal");
        Directory.CreateDirectory(teamDir);
        Directory.CreateDirectory(personalDir);

        var filePath = Path.Combine(personalDir, "settings.json");
        var result = SquadResolver.EnsureSquadPathTriple(filePath, _squadRoot, teamDir, personalDir);

        Assert.Equal(Path.GetFullPath(filePath), result);
    }

    [Fact]
    public void EnsureSquadPathTriple_OutsideAllRoots_ThrowsArgumentException()
    {
        var teamDir = Path.Combine(_testRoot, "team");
        Directory.CreateDirectory(teamDir);

        // Use a path outside all roots AND the system temp directory
        var outsidePath = OperatingSystem.IsWindows()
            ? @"C:\Windows\System32\outside.txt"
            : "/usr/local/outside-test.txt";

        Assert.Throws<ArgumentException>(() =>
            SquadResolver.EnsureSquadPathTriple(outsidePath, _squadRoot, teamDir, personalDir: null));
    }

    [Fact]
    public void EnsureSquadPathResolved_InsideProjectDir_ReturnsResolvedPath()
    {
        var paths = new ResolvedSquadPaths
        {
            Mode = SquadMode.Project,
            ProjectDir = _squadRoot,
            TeamDir = _squadRoot
        };

        var filePath = Path.Combine(_squadRoot, "output.txt");
        var result = SquadResolver.EnsureSquadPathResolved(filePath, paths);

        Assert.Equal(Path.GetFullPath(filePath), result);
    }
}

#endregion

#region SquadResolver — DeriveProjectKey

public sealed class DeriveProjectKeyTests
{
    [Theory]
    [InlineData("/home/user/My-Cool-Project", "my-cool-project")]
    [InlineData("/path/to/my project", "my-project")]
    [InlineData("C:\\Users\\tamir\\squad", "squad")]
    [InlineData("/", "unknown-project")]
    public void DeriveProjectKey_VariousPaths_ReturnsExpectedKey(string projectDir, string expectedKey)
    {
        var result = SquadResolver.DeriveProjectKey(projectDir);

        Assert.Equal(expectedKey, result);
    }

    [Fact]
    public void DeriveProjectKey_NormalPath_LowercasesBasename()
    {
        var result = SquadResolver.DeriveProjectKey("/home/user/MyProject");

        Assert.Equal("myproject", result);
    }

    [Fact]
    public void DeriveProjectKey_EmptyBasename_ReturnsUnknownProject()
    {
        // Path.GetFileName of just a root (or empty normalized path) → ""
        var result = SquadResolver.DeriveProjectKey("/");

        Assert.Equal("unknown-project", result);
    }
}

#endregion

#region SquadResolver — ResolveExternalStateDir

public sealed class ResolveExternalStateDirTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"squad-extstate-test-{Guid.NewGuid():N}");
    private readonly string? _origXdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
    private readonly string? _origAppData = Environment.GetEnvironmentVariable("APPDATA");

    public ResolveExternalStateDirTests()
    {
        Directory.CreateDirectory(_testRoot);
        // Redirect personal squad dir into test root so we never touch real user state
        if (OperatingSystem.IsWindows())
            Environment.SetEnvironmentVariable("APPDATA", _testRoot);
        else
            Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _testRoot);
    }

    public void Dispose()
    {
        // Restore env
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _origXdgConfig);
        Environment.SetEnvironmentVariable("APPDATA", _origAppData);
        if (Directory.Exists(_testRoot))
            Directory.Delete(_testRoot, recursive: true);
    }

    [Fact]
    public void ResolveExternalStateDir_ValidKey_CreatesDirectory()
    {
        var dir = SquadResolver.ResolveExternalStateDir("test-project-123");

        Assert.True(Directory.Exists(dir));
        Assert.Contains("projects", dir);
        Assert.Contains("test-project-123", dir);
    }

    [Fact]
    public void ResolveExternalStateDir_CreateFalse_ReturnsPathWithoutCreating()
    {
        var dir = SquadResolver.ResolveExternalStateDir("nonexistent-project-xyz", create: false);

        Assert.Contains("nonexistent-project-xyz", dir);
        Assert.Contains("projects", dir);
    }

    [Fact]
    public void ResolveExternalStateDir_SameKey_IsIdempotent()
    {
        var dir1 = SquadResolver.ResolveExternalStateDir("idempotent-test-proj");
        var dir2 = SquadResolver.ResolveExternalStateDir("idempotent-test-proj");

        Assert.Equal(dir1, dir2);
    }

    [Fact]
    public void ResolveExternalStateDir_PathTraversalKey_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            SquadResolver.ResolveExternalStateDir("../../etc/passwd"));
    }

    [Fact]
    public void ResolveExternalStateDir_EmptyKey_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            SquadResolver.ResolveExternalStateDir(""));
    }

    [Fact]
    public void ResolveExternalStateDir_KeyWithSlashes_SanitizesSlashes()
    {
        var dir = SquadResolver.ResolveExternalStateDir("my/project");

        // Slashes in the key should be replaced with dashes
        var projectsSubPath = Path.Combine("projects", "my-project");
        Assert.Contains(projectsSubPath, dir);
    }
}

#endregion

#region SquadDirConfig — StateLocation and StateBackend

public sealed class SquadDirConfigStateTests
{
    [Fact]
    public void SquadDirConfig_DefaultStateLocation_IsNull()
    {
        var config = new SquadDirConfig();

        Assert.Null(config.StateLocation);
        Assert.Null(config.StateBackend);
    }

    [Fact]
    public void SquadDirConfig_WithStateLocation_ReturnsValue()
    {
        var config = new SquadDirConfig { StateLocation = "external" };

        Assert.Equal("external", config.StateLocation);
    }

    [Fact]
    public void SquadDirConfig_WithStateBackend_ReturnsValue()
    {
        var config = new SquadDirConfig { StateBackend = "git-notes" };

        Assert.Equal("git-notes", config.StateBackend);
    }
}

#endregion

#region Platform — CommunicationTypes

public sealed class CommunicationTypesTests
{
    [Fact]
    public void CommunicationChannel_HasExpectedValues()
    {
        var values = Enum.GetValues<CommunicationChannel>();

        Assert.Contains(CommunicationChannel.GitHubDiscussions, values);
        Assert.Contains(CommunicationChannel.AdoWorkItems, values);
        Assert.Contains(CommunicationChannel.TeamsGraph, values);
        Assert.Contains(CommunicationChannel.FileLog, values);
    }

    [Fact]
    public void CommunicationReply_RequiredProperties_CanBeSet()
    {
        var reply = new CommunicationReply
        {
            Author = "Alice",
            Body = "LGTM!",
            Timestamp = DateTimeOffset.UtcNow,
            Id = "reply-001"
        };

        Assert.Equal("Alice", reply.Author);
        Assert.Equal("LGTM!", reply.Body);
        Assert.Equal("reply-001", reply.Id);
    }

    [Fact]
    public void CommunicationConfig_DefaultValues_AreCorrect()
    {
        var config = new CommunicationConfig { Channel = CommunicationChannel.FileLog };

        Assert.Equal(CommunicationChannel.FileLog, config.Channel);
        Assert.False(config.PostAfterSession);
        Assert.False(config.PostDecisions);
        Assert.False(config.PostEscalations);
        Assert.Null(config.AdapterConfig);
    }
}

#endregion

#region Platform — TeamsCommunicationAdapter (unit tests — no network)

public sealed class TeamsCommunicationAdapterUnitTests
{
    [Fact]
    public void TeamsCommunicationAdapter_DefaultConfig_HasTeamsGraphChannel()
    {
        var adapter = new TeamsCommunicationAdapter();

        Assert.Equal(CommunicationChannel.TeamsGraph, adapter.Channel);
    }

    [Fact]
    public void TeamsCommunicationAdapter_GetNotificationUrl_ReturnsTeamsDeepLink()
    {
        var adapter = new TeamsCommunicationAdapter();

        var url = adapter.GetNotificationUrl("chat-id-123");

        Assert.NotNull(url);
        Assert.Contains("teams.microsoft.com", url);
        Assert.Contains("chat-id-123", url);
    }

    [Fact]
    public void TeamsCommunicationAdapter_GetNotificationUrl_NullThreadId_ReturnsNull()
    {
        var adapter = new TeamsCommunicationAdapter();

        var url = adapter.GetNotificationUrl(null!);

        Assert.Null(url);
    }

    [Fact]
    public void TeamsCommunicationAdapter_EscapeHtml_EscapesSpecialChars()
    {
        Assert.Equal("&lt;b&gt;bold&lt;/b&gt; &amp; more", TeamsCommunicationAdapter.EscapeHtml("<b>bold</b> & more"));
    }

    [Fact]
    public void TeamsCommunicationAdapter_StripHtml_RemovesTags()
    {
        Assert.Equal("Hello world", TeamsCommunicationAdapter.StripHtml("<p>Hello <strong>world</strong></p>"));
    }

    [Fact]
    public void TeamsCommsConfig_DefaultValues_AreCorrect()
    {
        var config = new TeamsCommsConfig();

        Assert.Equal("organizations", config.TenantId);
        Assert.Equal("14d82eec-204b-4c2f-b7e8-296a70dab67e", config.ClientId);
        Assert.Null(config.RecipientUpn);
        Assert.Null(config.ChatId);
    }
}

#endregion
