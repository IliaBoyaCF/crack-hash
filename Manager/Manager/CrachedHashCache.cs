using Manager.Abstractions.Options;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Manager.Service;

public class CrachedHashCache : ICrackedHashCache
{

    private readonly ConcurrentDictionary<string, IEnumerable<string>> _cache = [];

    public int Count => _cache.Count;

    public int Capacity { get; init; }

    public CrachedHashCache(IOptions<CacheOptions> options)
    {
        Capacity = options.Value.Capacity;
        if (Capacity < 1)
        {
            throw new ArgumentException("Capacity can't be zero or less.");
        }
    }

    public bool TryAdd(string hash, IEnumerable<string> answers)
    {
        while (Count >= Capacity)
        {
            _cache.TryRemove(_cache.First().Key, out _);
        }
        return _cache.TryAdd(hash, [.. answers]);
    }

    public bool TryGetCached(string hash, out IEnumerable<string>? answers)
    {
        return _cache.TryGetValue(hash, out answers);
    }
}
