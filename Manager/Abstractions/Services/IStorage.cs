using Manager.Abstractions.Entities;

namespace Manager.Abstractions.Services;

public interface IStorage<TKey, TValue> where TKey : notnull
{
    Task<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default);
    Task<TValue?> TryGetAsync(TKey key, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(TKey key, CancellationToken cancellationToken = default);

    Task UpsertAsync(TKey key, TValue value, CancellationToken cancellationToken = default);

    Task DeleteAsync(TKey key, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TValue>> GetAllAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<TValue> GetAllAsyncEnumerable(CancellationToken cancellationToken = default);
}
