using Squad.SDK.NET.Agents;
using Xunit;

namespace Squad.SDK.NET.Tests;

public sealed class CharterCompilerTests : IDisposable
{
    private readonly string _tempDir;

    public CharterCompilerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"charter-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CompileAsync_ParsesFrontmatterCorrectly()
    {
        // Arrange
        var charterPath = Path.Combine(_tempDir, "charter.md");
        var content = @"---
name: test-agent
role: Backend Dev
expertise: [C#, .NET]
style: pragmatic
modelPreference: gpt-5
---
You are a backend developer.";

        await File.WriteAllTextAsync(charterPath, content);

        // Act
        var charter = await CharterCompiler.CompileAsync(charterPath);

        // Assert
        Assert.Equal("test-agent", charter.Name);
        Assert.Equal("Backend Dev", charter.Role);
        Assert.Equal(2, charter.Expertise.Count);
        Assert.Contains("C#", charter.Expertise);
        Assert.Contains(".NET", charter.Expertise);
        Assert.Equal("pragmatic", charter.Style);
        Assert.Equal("gpt-5", charter.ModelPreference);
        Assert.Equal("You are a backend developer.", charter.Prompt);
    }

    [Fact]
    public async Task CompileAsync_ExtractsBodyAsPrompt()
    {
        // Arrange
        var charterPath = Path.Combine(_tempDir, "charter.md");
        var content = @"---
name: test-agent
role: Tester
---
This is the agent prompt.
It can span multiple lines.
And contain various instructions.";

        await File.WriteAllTextAsync(charterPath, content);

        // Act
        var charter = await CharterCompiler.CompileAsync(charterPath);

        // Assert
        Assert.Contains("This is the agent prompt.", charter.Prompt);
        Assert.Contains("It can span multiple lines.", charter.Prompt);
        Assert.Contains("And contain various instructions.", charter.Prompt);
    }

    [Fact]
    public async Task CompileAsync_HandlesMissingOptionalFields()
    {
        // Arrange
        var charterPath = Path.Combine(_tempDir, "charter.md");
        var content = @"---
name: minimal-agent
role: Dev
---
Minimal charter.";

        await File.WriteAllTextAsync(charterPath, content);

        // Act
        var charter = await CharterCompiler.CompileAsync(charterPath);

        // Assert
        Assert.Equal("minimal-agent", charter.Name);
        Assert.Equal("Dev", charter.Role);
        Assert.Empty(charter.Expertise);
        Assert.Null(charter.Style);
        Assert.Null(charter.ModelPreference);
        Assert.Equal("Minimal charter.", charter.Prompt);
    }

    [Fact]
    public async Task CompileAsync_ParsesAllowedAndExcludedTools()
    {
        // Arrange
        var charterPath = Path.Combine(_tempDir, "charter.md");
        var content = @"---
name: tool-agent
role: Developer
allowedTools: [file-read, file-write, shell]
excludedTools: [delete, format]
---
Agent with tool restrictions.";

        await File.WriteAllTextAsync(charterPath, content);

        // Act
        var charter = await CharterCompiler.CompileAsync(charterPath);

        // Assert
        Assert.NotNull(charter.AllowedTools);
        Assert.Equal(3, charter.AllowedTools.Count);
        Assert.Contains("file-read", charter.AllowedTools);
        Assert.Contains("file-write", charter.AllowedTools);
        Assert.Contains("shell", charter.AllowedTools);

        Assert.NotNull(charter.ExcludedTools);
        Assert.Equal(2, charter.ExcludedTools.Count);
        Assert.Contains("delete", charter.ExcludedTools);
        Assert.Contains("format", charter.ExcludedTools);
    }

    [Fact]
    public async Task CompileAllAsync_LoadsMultipleCharters()
    {
        // Arrange
        var squadDir = Path.Combine(_tempDir, ".squad", "agents");
        Directory.CreateDirectory(Path.Combine(squadDir, "agent1"));
        Directory.CreateDirectory(Path.Combine(squadDir, "agent2"));
        Directory.CreateDirectory(Path.Combine(squadDir, "agent3"));

        await File.WriteAllTextAsync(Path.Combine(squadDir, "agent1", "charter.md"), @"---
name: agent1
role: Dev
---
Agent 1");

        await File.WriteAllTextAsync(Path.Combine(squadDir, "agent2", "charter.md"), @"---
name: agent2
role: QA
---
Agent 2");

        await File.WriteAllTextAsync(Path.Combine(squadDir, "agent3", "charter.md"), @"---
name: agent3
role: DevOps
---
Agent 3");

        // Act
        var charters = await CharterCompiler.CompileAllAsync(_tempDir);

        // Assert
        Assert.Equal(3, charters.Count);
        Assert.Contains(charters, c => c.Name == "agent1");
        Assert.Contains(charters, c => c.Name == "agent2");
        Assert.Contains(charters, c => c.Name == "agent3");
    }

    [Fact]
    public async Task CompileAsync_WithNoFrontmatter_UsesDefaults()
    {
        // Arrange
        var charterPath = Path.Combine(_tempDir, "charter.md");
        var content = "Just a plain prompt without frontmatter.";

        await File.WriteAllTextAsync(charterPath, content);

        // Act
        var charter = await CharterCompiler.CompileAsync(charterPath);

        // Assert
        Assert.Equal("agent", charter.Role); // Default role
        Assert.Equal("Just a plain prompt without frontmatter.", charter.Prompt);
    }

    [Fact]
    public async Task CompileAsync_WithDisplayName_ParsesCorrectly()
    {
        // Arrange
        var charterPath = Path.Combine(_tempDir, "charter.md");
        var content = @"---
name: backend-dev
displayName: Senior Backend Developer
role: Backend
---
I am a senior backend developer.";

        await File.WriteAllTextAsync(charterPath, content);

        // Act
        var charter = await CharterCompiler.CompileAsync(charterPath);

        // Assert
        Assert.Equal("backend-dev", charter.Name);
        Assert.Equal("Senior Backend Developer", charter.DisplayName);
    }

    [Fact]
    public async Task CompileAllAsync_EmptyDirectory_ReturnsEmptyList()
    {
        // Arrange
        var emptyDir = Path.Combine(_tempDir, "empty");
        Directory.CreateDirectory(emptyDir);

        // Act
        var charters = await CharterCompiler.CompileAllAsync(emptyDir);

        // Assert
        Assert.Empty(charters);
    }
}
