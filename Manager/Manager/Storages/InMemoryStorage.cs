using Manager.Abstractions.Services;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Manager.Service.Storages;

public class InMemoryStorage<TKey, TValue> : IStorage<TKey, TValue> where TKey : notnull
{

    private readonly ConcurrentDictionary<TKey, TValue> _storage = [];

    public Task DeleteAsync(TKey key, CancellationToken cancellationToken = default)
    {
        _storage.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(TKey key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_storage.ContainsKey(key));
    }

    public Task<IReadOnlyCollection<TValue>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var values = _storage.Values.ToList().AsReadOnly();
        return Task.FromResult<IReadOnlyCollection<TValue>>(values);
    }

    public async IAsyncEnumerable<TValue> GetAllAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var value in _storage.Values)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            yield return value;
            await Task.Yield();
        }
    }

    public Task<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
    {
        if (_storage.TryGetValue(key, out var value))
        {
            return Task.FromResult(value);
        }

        throw new KeyNotFoundException($"Key '{key}' not found");
    }

    public Task<TValue?> TryGetAsync(TKey key, CancellationToken cancellationToken = default)
    {
        _storage.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task UpsertAsync(TKey key, TValue value, CancellationToken cancellationToken = default)
    {
        _storage.AddOrUpdate(key, value, (_, _) => value);
        return Task.CompletedTask;
    }
}
