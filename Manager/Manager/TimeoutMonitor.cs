using Manager.Abstractions.Model;
using Manager.Abstractions.Options;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Manager.Service;

public class TimeoutMonitor<TKey>(IOptions<TimeoutOptions> options, ILogger<TimeoutMonitor<TKey>> logger) : BackgroundService, ITimeoutMonitor<TKey> where TKey : notnull
{

    private readonly ILogger<TimeoutMonitor<TKey>> _logger = logger;

    private readonly TimeSpan _checkInterval = options.Value.TimeoutCheckInterval;
    private readonly ConcurrentDictionary<TKey, ITimeoutable> _monitoringItemsDict = [];

    public IEnumerable<TKey> GetTrackedKeys()
    {
        return _monitoringItemsDict.Keys;
    }

    public bool TryAdd(TKey key, ITimeoutable timed, bool resetStartedAt = false)
    {
        if (resetStartedAt)
        {
            timed.ResetTimeout();
        }
        return _monitoringItemsDict.TryAdd(key, timed);
    }

    public bool TryRemove(TKey key)
    {
        var wasRemoved = _monitoringItemsDict.TryRemove(key, out var removed);
        if (wasRemoved)
        {
            removed?.IgnoreTimeout();
        }
        return wasRemoved;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_checkInterval, stoppingToken);
            CheckTimeouts();
        }
    }

    private void CheckTimeouts()
    {
        var now = DateTime.Now;
        foreach (var pair in _monitoringItemsDict)
        {
            if (!pair.Value.IsTimeoutEnabled)
            {
                _monitoringItemsDict.TryRemove(pair.Key, out var _);
                continue;
            }
            if (now - pair.Value.StartedAt < pair.Value.TimeoutInterval)
            {
                continue;
            }
            var wasRemoved = _monitoringItemsDict.TryRemove(pair.Key, out var removed);
            if (!wasRemoved)
            {
                continue;
            }
            removed?.OnTimeout();
        }
    }
}
