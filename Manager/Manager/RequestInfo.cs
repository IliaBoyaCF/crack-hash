using Manager.Abstractions.Model;

namespace Manager.Service;

internal class RequestInfo : IRequestInfo
{
    private readonly object _lock = new();
    
    private RequestStatus _status;

    private TimeSpan _timeoutInterval = TimeSpan.MaxValue;
    public TimeSpan TimeoutInterval 
    { 
        get => _timeoutInterval; 
        set
        {
            lock (_lock)
            {
                _timeoutInterval = value;
            }
        } 
    }

    public RequestStatus Status 
    { 
        get => _status; 
        set
        {
            EventHandler? completedEvent = null;
            lock (_lock)
            {
                var oldStatus = _status;
                if (IsFinalStatus(oldStatus) && !IsFinalStatus(value))
                {
                    throw new ArgumentException("Can't change status from final to non final.");
                }

                _status = value;

                if (IsFinalStatus(_status))
                {
                    IgnoreTimeout();
                    completedEvent = Completed;
                }
            }
            completedEvent?.Invoke(this, EventArgs.Empty);
        } 
    }

    private IEnumerable<string>? _data;
    public IEnumerable<string>? Data 
    { 
        get => _data; 
        set 
        {
            lock (_lock)
            {
                _data = value;
            }
        }
    }

    public Guid Id { get; init; }

    public required CrackRequest CrackRequest { get; init; }

    private bool _tracked = false;
    public bool IsTimeoutEnabled { get => _tracked; set => Interlocked.Exchange(ref _tracked, value); }

    private DateTime _monitoringStart = DateTime.MinValue;
    public DateTime StartedAt { get => _monitoringStart; }

    public event EventHandler? Timeout;
    public event EventHandler? Completed;

    private static bool IsFinalStatus(RequestStatus status)
    {
        return status switch
        {
            RequestStatus.READY => true,
            RequestStatus.READY_WITH_FAULTS => true,
            RequestStatus.ERROR => true,
            _ => false
        };
    }

    public void AddResults(IEnumerable<string> results)
    {
        lock (_lock)
        {
            if (Data == null)
            {
                Data = new List<string>();
            }
            ((List<string>)Data).AddRange(results);
        }
    }

    public void ResetTimeout()
    {
        lock (_lock)
        {
            _monitoringStart = DateTime.Now;
            _tracked = true;
        }
    }

    public void IgnoreTimeout()
    {
        Interlocked.Exchange(ref _tracked, false);
    }

    public void OnTimeout()
    {
        Interlocked.Exchange(ref _tracked, false);
        Timeout?.Invoke(this, EventArgs.Empty);
    }
}
