using Manager.Abstractions.Events;

namespace Manager.Abstractions.Services;

public interface IEventBus
{
    void Publish<T>(T @event) where T : IEvent;
    IDisposable Subscribe<T>(Action<T> handler) where T : IEvent;
}
