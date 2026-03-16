using Contracts.ManagerToWorker;
using Manager.Abstractions.Model;

namespace Manager.Service;

internal class WorkerTask : IWorkerTask, IDisposable
{

    private System.Timers.Timer _timer;
    private RequestStatus _status;
    private readonly object _lock = new object();
    private bool _disposed;

    public required CrackHashManagerRequest Request { get; init; }
    public required Uri WorkerAddress { get; init; }
    public RequestStatus Status 
    {
        get => _status; 
        set 
        {
            lock (_lock)
            {
                var oldStatus = _status;
                _status = value;
                
                if (_disposed)
                {
                    return;
                }

                if (oldStatus == RequestStatus.IN_PROGRESS && value != RequestStatus.IN_PROGRESS)
                {
                    _timer.Stop();
                    Dispose();
                }
            }
        } 
    }

    public required TimeSpan TimeoutInterval { get; init; }

    public event EventHandler? Timeout;

    public WorkerTask()
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
            if (_status == RequestStatus.IN_PROGRESS)
            {
                Timeout?.Invoke(this, EventArgs.Empty);
            }
            Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _timer?.Elapsed -= OnTimerElapsed;
        _timer?.Dispose();
    }

    public void StartTimeoutMonitoring()
    {
        if (_disposed || _status != RequestStatus.IN_PROGRESS)
        {
            return;
        }
        _timer.Stop();
        _timer.Interval = TimeoutInterval.TotalMilliseconds;
        _timer.Start();
    }
}
