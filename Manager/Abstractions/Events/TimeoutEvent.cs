using Manager.Abstractions.Model;

namespace Manager.Abstractions.Events;

public class TimeoutEvent : IEvent
{
    public required ITimeoutable Source { get; init; }
}
