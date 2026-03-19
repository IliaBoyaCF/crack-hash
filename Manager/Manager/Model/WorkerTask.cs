using Contracts.ManagerToWorker;
using Manager.Abstractions.Model;

namespace Manager.Service.Model;

internal class WorkerTask : IWorkerTask
{

    private readonly object _lock = new object();

    private RequestStatus _status;

    public required CrackHashManagerRequest Request { get; init; }
    public required Uri WorkerAddress { get; init; }
    public RequestStatus Status 
    {
        get => _status; 
        set 
        {
            Interlocked.Exchange(ref _status, value);
            if (value != RequestStatus.READY)
            {
                return;
            }
            IgnoreTimeout();
        } 
    }

    private bool _tracked = false;
    public bool IsTimeoutEnabled { get => _tracked; set => Interlocked.Exchange(ref _tracked, value); }

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

    private DateTime _monitoringStart = DateTime.MinValue;
    public DateTime StartedAt { get => _monitoringStart; }

    public event EventHandler? Timeout;

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
