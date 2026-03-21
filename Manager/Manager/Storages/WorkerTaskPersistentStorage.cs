using Manager.Abstractions.Model;
using Manager.Abstractions.Services;
using Manager.Service.Entities;
using Manager.Service.Extentions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;

namespace Manager.Service.Storages;

public class WorkerTaskPersistentStorage : MongoDBPersistentStorage<string, List<IWorkerTask>, WorkerTaskListEntity>, ITaskStorage
{
    public WorkerTaskPersistentStorage(
        ILogger<MongoDBPersistentStorage<string, List<IWorkerTask>, WorkerTaskListEntity>> logger,
        DB db)
        : this((k, tl) => tl.ToEntity(k), logger, (k, el) => el.WorkerTasks.Select(e => e.ToModel(k)).ToList(), db)
    {
    }

    private WorkerTaskPersistentStorage(
        Func<string, List<IWorkerTask>, WorkerTaskListEntity> toEntity,
        ILogger<MongoDBPersistentStorage<string, List<IWorkerTask>, WorkerTaskListEntity>> logger,
        Func<string, WorkerTaskListEntity, List<IWorkerTask>> toValue,
        DB db)
        : base(toEntity, logger, toValue, db)
    {
    }

    public virtual async Task UpdateTaskStatusAsync(
    string requestId,
    int partNumber,
    RequestStatus newStatus,
    CancellationToken cancellationToken = default)
    {
        try
        {
            var taskId = $"{requestId}_{partNumber}";

            var filter = Builders<WorkerTaskListEntity>.Filter.Eq(e => e.ID, GetEntityId(requestId));
            var update = Builders<WorkerTaskListEntity>.Update
                .Set("WorkerTasks.$[elem].Status", newStatus);

            var arrayFilter = new BsonDocumentArrayFilterDefinition<BsonDocument>(
                new BsonDocument("elem._id", taskId));

            var options = new UpdateOptions
            {
                ArrayFilters = new[] { arrayFilter }
            };

            var collection = _db.Database().GetCollection<WorkerTaskListEntity>("WorkerTaskListEntity");
            var result = await collection.UpdateOneAsync(filter, update, options, cancellationToken);

            if (result.MatchedCount == 0)
            {
                throw new KeyNotFoundException($"Request {requestId} or task {partNumber} not found");
            }

            _logger.LogDebug("Fast updated task {RequestId}/{PartNumber} status to {Status}",
                requestId, partNumber, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fast updating task {RequestId}/{PartNumber} status",
                requestId, partNumber);
            throw;
        }
    }
}

public static class ListMapperExtention
{
    public static WorkerTaskListEntity ToEntity(this List<IWorkerTask> list, string key)
    {
        return new WorkerTaskListEntity
        {
            ID = key,
            WorkerTasks = list.Select(t => t.ToEntity(key)).ToList(),
        };
    }
}
