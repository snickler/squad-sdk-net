using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Extensions;

namespace Squad.SDK.NET.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services;
    }

    [Fact]
    public void AddSquadSdk_RegistersIEventBus()
    {
        // Arrange
        var services = CreateServices();

        // Act
        services.AddSquadSdk();
        var provider = services.BuildServiceProvider();

        // Assert
        var eventBus = provider.GetService<IEventBus>();
        Assert.NotNull(eventBus);
    }

    [Fact]
    public void AddSquadSdk_RegistersIHookPipeline()
    {
        // Arrange
        var services = CreateServices();

        // Act
        services.AddSquadSdk();
        var provider = services.BuildServiceProvider();

        // Assert
        var hookPipeline = provider.GetService<IHookPipeline>();
        Assert.NotNull(hookPipeline);
    }

    [Fact]
    public void AddSquadSdk_RegistersISquadClient()
    {
        // Arrange
        var services = CreateServices();

        // Act
        services.AddSquadSdk();
        var provider = services.BuildServiceProvider();

        // Assert
        var client = provider.GetService<ISquadClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddSquadSdk_RegistersIAgentSessionManager()
    {
        // Arrange
        var services = CreateServices();

        // Act
        services.AddSquadSdk();
        var provider = services.BuildServiceProvider();

        // Assert
        var agentManager = provider.GetService<IAgentSessionManager>();
        Assert.NotNull(agentManager);
    }

    [Fact]
    public void AddSquadSdk_RegistersICoordinator()
    {
        // Arrange
        var services = CreateServices();

        // Act - Need to provide valid configuration for coordinator
        services.AddSquadSdk(builder =>
        {
            builder.WithTeam(team => team.Name("Test Team"));
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var coordinator = provider.GetService<ICoordinator>();
        Assert.NotNull(coordinator);
    }

    [Fact]
    public void AddSquadSdk_CustomConfiguration_InvokesCallback()
    {
        // Arrange
        var services = CreateServices();
        var callbackInvoked = false;

        // Act
        services.AddSquadSdk(builder =>
        {
            callbackInvoked = true;
            builder.WithTeam(team => team.Name("Test Team"));
        });

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void AddSquadSdk_WithoutConfiguration_DoesNotThrow()
    {
        // Arrange
        var services = CreateServices();

        // Act & Assert - AddSquadSdk without config should not throw
        var exception = Record.Exception(() =>
        {
            services.AddSquadSdk();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void AddSquadSdk_RegistersSingleton_EventBus()
    {
        // Arrange
        var services = CreateServices();
        services.AddSquadSdk();
        var provider = services.BuildServiceProvider();

        // Act
        var eventBus1 = provider.GetService<IEventBus>();
        var eventBus2 = provider.GetService<IEventBus>();

        // Assert
        Assert.Same(eventBus1, eventBus2);
    }

    [Fact]
    public void AddSquadSdk_RegistersSingleton_HookPipeline()
    {
        // Arrange
        var services = CreateServices();
        services.AddSquadSdk();
        var provider = services.BuildServiceProvider();

        // Act
        var hookPipeline1 = provider.GetService<IHookPipeline>();
        var hookPipeline2 = provider.GetService<IHookPipeline>();

        // Assert
        Assert.Same(hookPipeline1, hookPipeline2);
    }

    [Fact]
    public void AddSquadSdk_RegistersSingleton_SquadClient()
    {
        // Arrange
        var services = CreateServices();
        services.AddSquadSdk();
        var provider = services.BuildServiceProvider();

        // Act
        var client1 = provider.GetService<ISquadClient>();
        var client2 = provider.GetService<ISquadClient>();

        // Assert
        Assert.Same(client1, client2);
    }
}
