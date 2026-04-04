using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Squad.SDK.NET.Abstractions;
using Squad.SDK.NET.Builder;
using Squad.SDK.NET.Casting;
using Squad.SDK.NET.Config;
using Squad.SDK.NET.Events;
using Squad.SDK.NET.Hooks;
using Squad.SDK.NET.Remote;
using Squad.SDK.NET.Resolution;
using Squad.SDK.NET.Sharing;
using Squad.SDK.NET.Skills;
using Squad.SDK.NET.State;
using Squad.SDK.NET.Storage;

namespace Squad.SDK.NET.Extensions;

/// <summary>
/// Extension methods for registering Squad SDK services with <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Squad SDK services using in-memory storage.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional squad builder configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSquadSdk(
        this IServiceCollection services,
        Action<SquadBuilder>? configure = null)
    {
        return AddSquadSdk(services, configure, useFileSystemStorage: false);
    }

    /// <summary>
    /// Registers all Squad SDK services with the option to use file-system-based storage.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional squad builder configuration action.</param>
    /// <param name="useFileSystemStorage">When <see langword="true"/>, uses file-system storage instead of in-memory.</param>
    /// <param name="storagePath">Optional custom storage directory path; defaults to a platform-specific local application data folder.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSquadSdk(
        this IServiceCollection services,
        Action<SquadBuilder>? configure,
        bool useFileSystemStorage,
        string? storagePath = null)
    {
        // Build and register SquadConfig if a builder action is provided
        if (configure is not null)
        {
            var builder = SquadBuilder.Create();
            configure(builder);
            var config = builder.Build();
            services.AddSingleton(config);
        }

        // Storage provider
        if (useFileSystemStorage)
        {
            services.AddSingleton<IStorageProvider>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<FileSystemStorageProvider>>();
                var path = storagePath ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Squad", "squad-storage");
                return new FileSystemStorageProvider(path, logger);
            });
        }
        else
        {
            services.AddSingleton<IStorageProvider, InMemoryStorageProvider>();
        }

        // State management
        services.AddSingleton<SquadState>();

        // Core infrastructure with logger support
        services.AddSingleton<IEventBus>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<EventBus>>();
            return new EventBus(logger);
        });
        services.AddSingleton<IHookPipeline>(_ => new HookPipeline());
        services.AddSingleton<SkillRegistry>();

        // Hooks
        services.AddSingleton<ReviewerLockoutHook>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ReviewerLockoutHook>>();
            return new ReviewerLockoutHook(logger);
        });
        services.AddSingleton<PiiScrubberHook>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<PiiScrubberHook>>();
            return new PiiScrubberHook(logger);
        });

        // Casting engine
        services.AddSingleton<CastingEngine>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<CastingEngine>>();
            var config = sp.GetService<SquadConfig>();
            return new CastingEngine(config?.Casting, logger);
        });

        // Resolution & Multi-squad
        services.AddSingleton<MultiSquadManager>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<MultiSquadManager>>();
            return new MultiSquadManager(logger);
        });

        // Sharing
        services.AddSingleton<SquadExporter>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SquadExporter>>();
            return new SquadExporter(logger);
        });
        services.AddSingleton<SquadImporter>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SquadImporter>>();
            return new SquadImporter(logger);
        });

        // Remote bridge
        services.AddSingleton<RemoteBridge>(sp =>
        {
            var client = sp.GetRequiredService<ISquadClient>();
            var logger = sp.GetRequiredService<ILogger<RemoteBridge>>();
            return new RemoteBridge(client, logger);
        });

        // Client and session management with logger support
        services.AddSingleton<ISquadClient>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new SquadClient(loggerFactory);
        });
        services.AddSingleton<IAgentSessionManager>(sp =>
        {
            var client = sp.GetRequiredService<ISquadClient>();
            var eventBus = sp.GetRequiredService<IEventBus>();
            var logger = sp.GetRequiredService<ILogger<Agents.AgentSessionManager>>();
            return new Agents.AgentSessionManager(client, eventBus, logger);
        });
        services.AddSingleton<ICoordinator>(sp =>
        {
            var config = sp.GetRequiredService<SquadConfig>();
            var agentManager = sp.GetRequiredService<IAgentSessionManager>();
            var eventBus = sp.GetRequiredService<IEventBus>();
            var logger = sp.GetRequiredService<ILogger<Coordinator.Coordinator>>();
            return new Coordinator.Coordinator(config, agentManager, eventBus, logger);
        });

        return services;
    }
}
