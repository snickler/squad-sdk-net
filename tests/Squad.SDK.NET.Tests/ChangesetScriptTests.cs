using System.Diagnostics;
using System.Text;

namespace Squad.SDK.NET.Tests;

public sealed class ChangesetScriptTests : IDisposable
{
    private readonly string _repoRoot;
    private readonly string _tempRoot;

    public ChangesetScriptTests()
    {
        _repoRoot = FindRepoRoot();
        _tempRoot = Directory.CreateTempSubdirectory("changeset-script-tests-").FullName;
    }

    [Fact]
    public void Status_WithPendingChangesets_ReportsHighestBumpAndNextVersion()
    {
        CreateRepoFixture("1.2.3");
        WriteChangeset("alpha-change.md", """
            ---
            "Squad.SDK.NET": patch
            ---

            Fix a regression in promotion state handling.
            """);
        WriteChangeset("beta-change.md", """
            ---
            "Squad.SDK.NET": minor
            ---

            Add preview validation for pending release changes.
            """);

        var result = RunScript("-Operation status");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("PendingChangesets: 2", result.StdOut);
        Assert.Contains("ReleaseType: minor", result.StdOut);
        Assert.Contains("NextVersion: 1.3.0", result.StdOut);
    }

    [Fact]
    public void Apply_WithPendingChangesets_BumpsVersionUpdatesChangelogAndDeletesChangesets()
    {
        CreateRepoFixture("0.4.0");
        WriteChangeset("release-flow.md", """
            ---
            "Squad.SDK.NET": patch
            ---

            Switch stable releases to automatic tagging from main.
            """);

        var result = RunScript("-Operation apply -ReleaseDate 2026-05-15");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("NextVersion: 0.4.1", result.StdOut);

        var project = File.ReadAllText(Path.Combine(_tempRoot, "src", "Squad.SDK.NET", "Squad.SDK.NET.csproj"));
        Assert.Contains("<Version>0.4.1</Version>", project);

        var changelog = File.ReadAllText(Path.Combine(_tempRoot, "CHANGELOG.md"));
        Assert.Contains("## [0.4.1] - 2026-05-15", changelog);
        Assert.Contains("Switch stable releases to automatic tagging from main.", changelog);

        Assert.False(File.Exists(Path.Combine(_tempRoot, ".changeset", "release-flow.md")));
    }

    [Fact]
    public void Status_WithRequireNoPending_FailsWhenChangesetsExist()
    {
        CreateRepoFixture("2.0.0");
        WriteChangeset("breaking-change.md", """
            ---
            "Squad.SDK.NET": major
            ---

            Remove the legacy insider promotion path.
            """);

        var result = RunScript("-Operation status -RequireNoPending");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Pending changesets are still present", result.StdErr);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private void CreateRepoFixture(string version)
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot, ".changeset"));
        Directory.CreateDirectory(Path.Combine(_tempRoot, "src", "Squad.SDK.NET"));

        File.WriteAllText(
            Path.Combine(_tempRoot, ".changeset", "README.md"),
            "# Changesets\n",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        File.WriteAllText(
            Path.Combine(_tempRoot, "src", "Squad.SDK.NET", "Squad.SDK.NET.csproj"),
            $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Version>{{version}}</Version>
              </PropertyGroup>
            </Project>
            """,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        File.WriteAllText(
            Path.Combine(_tempRoot, "CHANGELOG.md"),
            """
            # Changelog

            All notable changes to this project will be documented in this file.

            ## [0.1.0] - 2026-04-04

            ### Added
            - Initial release
            """,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private void WriteChangeset(string fileName, string content)
    {
        File.WriteAllText(
            Path.Combine(_tempRoot, ".changeset", fileName),
            content.Replace("\r\n", "\n"),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private ScriptResult RunScript(string arguments)
    {
        var scriptPath = Path.Combine(_repoRoot, "scripts", "Changesets.ps1");
        var startInfo = new ProcessStartInfo
        {
            FileName = "pwsh",
            Arguments = $"-NoProfile -File \"{scriptPath}\" -RepoRoot \"{_tempRoot}\" {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        Assert.NotNull(process);

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ScriptResult(process.ExitCode, stdout, stderr);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Squad.SDK.NET.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }

    private sealed record ScriptResult(int ExitCode, string StdOut, string StdErr);
}
