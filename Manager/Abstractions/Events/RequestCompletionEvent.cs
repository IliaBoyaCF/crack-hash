using Manager.Abstractions.Model;

namespace Manager.Abstractions.Events;

public class RequestCompletionEvent : IEvent
{
    public required IRequestInfo Source { get; init; }
}
