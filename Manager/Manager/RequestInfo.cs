using Manager.Abstractions.Model;
using System.Text.Json.Serialization;

namespace Manager.Service;

public class RequestInfo : IRequestInfo, IDisposable
{

    private System.Timers.Timer _timer;
    private RequestStatus _status;
    private readonly object _lock = new object();
    private bool _disposed;

    [JsonIgnore]
    public TimeSpan TimeoutInterval { get; init; } = TimeSpan.MaxValue;
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
    public IEnumerable<string>? Data { get; set; }

    public DateTime? CreatedTime { get; private set; }

    public event EventHandler? Timeout;

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

    public void StartTimoutMonitoring()
    {
        if (_disposed || _status != RequestStatus.IN_PROGRESS)
        {
            return;
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
