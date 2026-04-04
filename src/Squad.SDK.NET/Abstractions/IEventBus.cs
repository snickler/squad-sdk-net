using Squad.SDK.NET.Events;

namespace Squad.SDK.NET.Abstractions;

public interface IEventBus
{
    IDisposable Subscribe(SquadEventType eventType, Func<SquadEvent, Task> handler);
    IDisposable SubscribeAll(Func<SquadEvent, Task> handler);
    Task EmitAsync(SquadEvent squadEvent, CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}
