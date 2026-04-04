using Squad.SDK.NET.Coordinator;

namespace Squad.SDK.NET.Abstractions;

public interface ICoordinator
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<RoutingDecision> RouteAsync(string message, CancellationToken cancellationToken = default);
    Task ExecuteAsync(RoutingDecision decision, string message, CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}
