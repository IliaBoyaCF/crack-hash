using Manager.Abstractions.Model;

namespace Manager.Service;

public class RequestInfo : IRequestInfo, IDisposable
{

    private readonly System.Timers.Timer _timer;
    private readonly object _lock = new();
    
    private RequestStatus _status;
    private bool _disposed;

    public TimeSpan TimeoutInterval { get; init; } = TimeSpan.MaxValue;
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

                if (_disposed)
                {
                    return;
                }

                if (IsFinalStatus(_status))
                {
                    CancelTimoutMonitoring();
                    completedEvent = Completed;
                }
            }
            completedEvent?.Invoke(this, EventArgs.Empty);
        } 
    }

    private void CancelTimoutMonitoring()
    {
        Dispose();
    }

    public IEnumerable<string>? Data { get; set; }

    public DateTime? CreatedTime { get; private set; }

    public Guid Id { get; init; }

    public required CrackRequest CrackRequest { get; init; }

    public event EventHandler? Timeout;
    public event EventHandler? Completed;

    public RequestInfo()
    {
        _timer = new System.Timers.Timer();
        _timer.AutoReset = false;
        _timer.Elapsed += OnTimerElapsed;
    }

    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        lock (_lock)
        {
            if (_disposed) { return; }
            if (!IsFinalStatus(_status))
            {
                Timeout?.Invoke(this, EventArgs.Empty);
            }
            Dispose();
        }
    }

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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _timer.Stop();
        _timer.Elapsed -= OnTimerElapsed;
        _timer.Dispose();
    }

    public void StartTimoutMonitoring()
    {
        if (_disposed || _status != RequestStatus.IN_PROGRESS)
        {
            throw new InvalidOperationException("Timeout has been already triggered or cancelled.");
        }
        CreatedTime = DateTime.Now;
        _timer.Stop();
        _timer.Interval = TimeoutInterval.TotalMilliseconds;
        _timer.Start();
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
}
