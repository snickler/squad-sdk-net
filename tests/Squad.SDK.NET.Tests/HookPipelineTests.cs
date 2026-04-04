using Squad.SDK.NET.Hooks;

namespace Squad.SDK.NET.Tests;

public sealed class HookPipelineTests
{
    [Fact]
    public async Task NoHooks_ReturnsAllow_ByDefault()
    {
        // Arrange
        var pipeline = new HookPipeline();
        var context = new PreToolUseContext
        {
            ToolName = "test_tool",
            Arguments = new Dictionary<string, object?> { ["arg1"] = "value1" },
            AgentName = "test-agent",
            SessionId = "session1"
        };

        // Act
        var result = await pipeline.RunPreToolHooksAsync(context);

        // Assert
        Assert.Equal(HookAction.Allow, result.Action);
    }

    [Fact]
    public async Task SinglePreHook_Allow_PassesThrough()
    {
        // Arrange
        var pipeline = new HookPipeline();
        pipeline.AddPreToolHook(ctx => Task.FromResult(PreToolUseResult.Allow()));
        var context = new PreToolUseContext
        {
            ToolName = "test_tool",
            Arguments = new Dictionary<string, object?> { ["arg1"] = "value1" },
            AgentName = "test-agent",
            SessionId = "session1"
        };

        // Act
        var result = await pipeline.RunPreToolHooksAsync(context);

        // Assert
        Assert.Equal(HookAction.Allow, result.Action);
    }

    [Fact]
    public async Task SinglePreHook_Block_StopsExecution()
    {
        // Arrange
        var pipeline = new HookPipeline();
        pipeline.AddPreToolHook(ctx => Task.FromResult(PreToolUseResult.Block("Test block reason")));
        var context = new PreToolUseContext
        {
            ToolName = "test_tool",
            Arguments = new Dictionary<string, object?> { ["arg1"] = "value1" },
            AgentName = "test-agent",
            SessionId = "session1"
        };

        // Act
        var result = await pipeline.RunPreToolHooksAsync(context);

        // Assert
        Assert.Equal(HookAction.Block, result.Action);
        Assert.Equal("Test block reason", result.Reason);
    }

    [Fact]
    public async Task SinglePreHook_Modify_ChangesArguments()
    {
        // Arrange
        var pipeline = new HookPipeline();
        pipeline.AddPreToolHook(ctx =>
        {
            var modified = new Dictionary<string, object?> { ["arg1"] = "modified_value" };
            return Task.FromResult(PreToolUseResult.Modify(modified));
        });
        var context = new PreToolUseContext
        {
            ToolName = "test_tool",
            Arguments = new Dictionary<string, object?> { ["arg1"] = "value1" },
            AgentName = "test-agent",
            SessionId = "session1"
        };

        // Act
        var result = await pipeline.RunPreToolHooksAsync(context);

        // Assert
        Assert.Equal(HookAction.Modify, result.Action);
        Assert.NotNull(result.ModifiedArguments);
        Assert.Equal("modified_value", result.ModifiedArguments["arg1"]);
    }

    [Fact]
    public async Task MultiplePreHooks_ChainInOrder()
    {
        // Arrange
        var pipeline = new HookPipeline();
        var executionOrder = new List<int>();
        
        pipeline.AddPreToolHook(ctx =>
        {
            executionOrder.Add(1);
            var modified = new Dictionary<string, object?> { ["step"] = "1" };
            return Task.FromResult(PreToolUseResult.Modify(modified));
        });
        
        pipeline.AddPreToolHook(ctx =>
        {
            executionOrder.Add(2);
            Assert.Equal("1", ctx.Arguments["step"]);
            var modified = new Dictionary<string, object?> { ["step"] = "2" };
            return Task.FromResult(PreToolUseResult.Modify(modified));
        });

        var context = new PreToolUseContext
        {
            ToolName = "test_tool",
            Arguments = new Dictionary<string, object?> { ["step"] = "0" },
            AgentName = "test-agent",
            SessionId = "session1"
        };

        // Act
        var result = await pipeline.RunPreToolHooksAsync(context);

        // Assert
        Assert.Equal(new[] { 1, 2 }, executionOrder);
        Assert.Equal("2", result.ModifiedArguments!["step"]);
    }

    [Fact]
    public async Task MultiplePreHooks_FirstBlock_ShortCircuits()
    {
        // Arrange
        var pipeline = new HookPipeline();
        var secondHookExecuted = false;
        
        pipeline.AddPreToolHook(ctx => Task.FromResult(PreToolUseResult.Block("First hook blocks")));
        pipeline.AddPreToolHook(ctx =>
        {
            secondHookExecuted = true;
            return Task.FromResult(PreToolUseResult.Allow());
        });

        var context = new PreToolUseContext
        {
            ToolName = "test_tool",
            Arguments = new Dictionary<string, object?>(),
            AgentName = "test-agent",
            SessionId = "session1"
        };

        // Act
        var result = await pipeline.RunPreToolHooksAsync(context);

        // Assert
        Assert.Equal(HookAction.Block, result.Action);
        Assert.False(secondHookExecuted);
    }

    [Fact]
    public async Task PostHooks_Run_AndReturnResults()
    {
        // Arrange
        var pipeline = new HookPipeline();
        var hookExecuted = false;
        pipeline.AddPostToolHook(ctx =>
        {
            hookExecuted = true;
            return Task.FromResult(PostToolUseResult.Ok());
        });

        var context = new PostToolUseContext
        {
            ToolName = "test_tool",
            Arguments = new Dictionary<string, object?>(),
            Result = "test result",
            AgentName = "test-agent",
            SessionId = "session1"
        };

        // Act
        var result = await pipeline.RunPostToolHooksAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.True(hookExecuted);
    }

    [Fact]
    public async Task PostHooks_Fail_ReturnsFailure()
    {
        // Arrange
        var pipeline = new HookPipeline();
        pipeline.AddPostToolHook(ctx => Task.FromResult(PostToolUseResult.Fail("Test failure")));

        var context = new PostToolUseContext
        {
            ToolName = "test_tool",
            Arguments = new Dictionary<string, object?>(),
            Result = "test result",
            AgentName = "test-agent",
            SessionId = "session1"
        };

        // Act
        var result = await pipeline.RunPostToolHooksAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Test failure", result.Message);
    }

    [Fact]
    public async Task PolicyConfig_BlockedCommands_BlocksCommand()
    {
        // Arrange
        var policy = new PolicyConfig
        {
            BlockedCommands = new[] { "rm -rf", "format" }
        };
        var pipeline = new HookPipeline(policy);
        var context = new PreToolUseContext
        {
            ToolName = "bash",
            Arguments = new Dictionary<string, object?> { ["command"] = "rm -rf /important" },
            AgentName = "test-agent",
            SessionId = "session1"
        };

        // Act
        var result = await pipeline.RunPreToolHooksAsync(context);

        // Assert
        Assert.Equal(HookAction.Block, result.Action);
        Assert.Contains("rm -rf", result.Reason);
    }

    [Fact]
    public async Task PolicyConfig_AllowedWritePaths_AllowsCorrectPath()
    {
        // Arrange
        var policy = new PolicyConfig
        {
            AllowedWritePaths = new[] { "/allowed/path" }
        };
        var pipeline = new HookPipeline(policy);
        var context = new PreToolUseContext
        {
            ToolName = "write_file",
            Arguments = new Dictionary<string, object?> { ["path"] = "/allowed/path/file.txt" },
            AgentName = "test-agent",
            SessionId = "session1"
        };

        // Act
        var result = await pipeline.RunPreToolHooksAsync(context);

        // Assert
        Assert.Equal(HookAction.Allow, result.Action);
    }

    [Fact]
    public async Task PolicyConfig_AllowedWritePaths_BlocksDisallowedPath()
    {
        // Arrange
        var policy = new PolicyConfig
        {
            AllowedWritePaths = new[] { "/allowed/path" }
        };
        var pipeline = new HookPipeline(policy);
        var context = new PreToolUseContext
        {
            ToolName = "write_file",
            Arguments = new Dictionary<string, object?> { ["path"] = "/disallowed/path/file.txt" },
            AgentName = "test-agent",
            SessionId = "session1"
        };

        // Act
        var result = await pipeline.RunPreToolHooksAsync(context);

        // Assert
        Assert.Equal(HookAction.Block, result.Action);
    }

    [Fact]
    public async Task NoPostHooks_ReturnsOk_ByDefault()
    {
        // Arrange
        var pipeline = new HookPipeline();
        var context = new PostToolUseContext
        {
            ToolName = "test_tool",
            Arguments = new Dictionary<string, object?>(),
            Result = "test result",
            AgentName = "test-agent",
            SessionId = "session1"
        };

        // Act
        var result = await pipeline.RunPostToolHooksAsync(context);

        // Assert
        Assert.True(result.Success);
    }
}
