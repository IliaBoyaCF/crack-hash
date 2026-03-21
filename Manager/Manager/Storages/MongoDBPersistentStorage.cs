using Manager.Abstractions.Services;
using Microsoft.Extensions.Logging;
using MongoDB.Entities;
using System.Runtime.CompilerServices;

namespace Manager.Service.Storages;

public abstract class MongoDBPersistentStorage<TKey, TValue, TEntity> : IStorage<TKey, TValue>
    where TKey : notnull
    where TEntity : Entity
{
    private readonly Func<TKey, TValue, TEntity> _toEntity;
    private readonly Func<TKey, TEntity, TValue> _toValue;
    private readonly ILogger<MongoDBPersistentStorage<TKey, TValue, TEntity>> _logger;

    private readonly DB _db;

    protected MongoDBPersistentStorage(
        Func<TKey, TValue, TEntity> toEntity,
        ILogger<MongoDBPersistentStorage<TKey, TValue, TEntity>> logger,
        Func<TKey, TEntity, TValue> toValue,
        DB db)
    {
        _toEntity = toEntity ?? throw new ArgumentNullException(nameof(toEntity));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _toValue = toValue ?? throw new ArgumentNullException(nameof(toValue));
        _db = db;
    }

    protected virtual string GetEntityId(TKey key)
        => key?.ToString() ?? throw new ArgumentNullException(nameof(key));

    protected virtual string CollectionName => typeof(TEntity).Name;

    public virtual async Task UpsertAsync(TKey key, TValue value, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = _toEntity(key, value);
            entity.ID = GetEntityId(key);

            await _db.SaveAsync(entity, cancellationToken);

            _logger.LogDebug("Entity saved successfully with {Key} and type {EntityType}",
                key, typeof(TEntity).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while saving entity with key {Key}", key);
            throw;
        }
    }

    public virtual async Task<bool> ExistsAsync(TKey key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var exists = await _db.Find<TEntity>()
                .MatchID(GetEntityId(key))
                .ExecuteAnyAsync(cancellationToken);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking key {Key}", key);
            throw;
        }
    }

    public virtual async Task<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var entity = await _db.Find<TEntity>()
                .MatchID(GetEntityId(key))
                .ExecuteFirstAsync(cancellationToken);

            if (entity == null)
            {
                throw new KeyNotFoundException($"Entity with key {key} is not found");
            }

            return _toValue(key, entity);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            _logger.LogError(ex, "Error getting entity with key {Key}", key);
            throw;
        }
    }

    public virtual async Task<TValue?> TryGetAsync(TKey key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var entity = await _db.Find<TEntity>()
                .MatchID(GetEntityId(key))
                .ExecuteFirstAsync(cancellationToken);

            return entity == null ? default : _toValue(key, entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity with key {Key}", key);
            throw;
        }
    }

    public virtual async Task DeleteAsync(TKey key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            await _db.DeleteAsync<TEntity>(GetEntityId(key), cancellationToken);
            _logger.LogDebug("Entity with key {Key} was deleted", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity with {Key}", key);
            throw;
        }
    }

    public virtual async Task<IReadOnlyCollection<TValue>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _db.Find<TEntity>()
                .ExecuteAsync(cancellationToken);

            return entities
                .Select(e => _toValue(ParseKey(e.ID), e))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all entities of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async IAsyncEnumerable<TValue> GetAllAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<TEntity> entities;

        try
        {
            entities = await _db.Find<TEntity>()
                .ExecuteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all entities of type {EntityType}", typeof(TEntity).Name);
            throw;
        }

        foreach (var entity in entities)
        {
            yield return _toValue(ParseKey(entity.ID), entity);
        }
    }

    protected virtual TKey ParseKey(string id)
    {
        try
        {
            if (typeof(TKey) == typeof(Guid))
                return (TKey)(object)Guid.Parse(id);

            if (typeof(TKey) == typeof(int))
                return (TKey)(object)int.Parse(id);

            if (typeof(TKey) == typeof(long))
                return (TKey)(object)long.Parse(id);

            if (typeof(TKey) == typeof(string))
                return (TKey)(object)id;

            return (TKey)Convert.ChangeType(id, typeof(TKey));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Couldn't convert string '{id}' in {typeof(TKey).Name} type. " +
                $"Try override method ParseKey for custom parsing logic.", ex);
        }
    }

    public virtual async Task<IReadOnlyCollection<TValue>> GetManyAsync(
        IEnumerable<TKey> keys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        try
        {
            var ids = keys.Select(GetEntityId).ToList();

            var entities = await _db.Find<TEntity>()
                .ManyAsync(e => ids.Contains(e.ID), cancellationToken);

            return entities
                .Select(e => _toValue(ParseKey(e.ID), e))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting many entities");
            throw;
        }
    }
}