using Manager.Abstractions.Events;
using Manager.Abstractions.Services;

namespace Manager.Service.Services;

public class EventBus : IEventBus
{
    private readonly object _lock = new object();

    private readonly Dictionary<Type, List<Subscribtion>> _subscriptions = [];

    public void Publish<T>(T @event) where T : IEvent
    {
        List<Subscribtion> handlersToCall;
        lock (_lock)
        {
            var eventType = typeof(T);

            if (!_subscriptions.ContainsKey(eventType))
            {
                return;
            }
            handlersToCall = _subscriptions[eventType].ToList();
        }

        foreach (var handler in handlersToCall)
        {
            handler.Invoke(@event);
        }

    }

    public IDisposable Subscribe<T>(Action<T> handler) where T : IEvent
    {
        var eventType = typeof(T);
        lock (_lock)
        {
            if (!_subscriptions.ContainsKey(eventType))
            {
                _subscriptions.Add(eventType, new List<Subscribtion>());
            }
            var subscription = new Subscribtion(this, handler);
            _subscriptions[eventType].Add(subscription);
            return subscription;
        }
    }

    private void RemoveSubscribtion(Subscribtion subscribtion)
    {
        lock (_lock)
        {
            foreach (var list in _subscriptions.Values)
            {
                if (list.Remove(subscribtion))
                {
                    break;
                }
            }
        }
    }

    private class Subscribtion : IDisposable
    {

        private readonly EventBus _eventBus;
        private readonly Delegate _handler;
        private bool _disposed;

        public Subscribtion(EventBus eventBus, Delegate handler)
        {
            _eventBus = eventBus;
            _handler = handler;
        }

        public void Invoke<T>(T @event) where T : IEvent
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
            ((Action<T>)_handler).Invoke(@event);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _eventBus.RemoveSubscribtion(this);
        }
    }
}
