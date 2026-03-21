using Manager.Abstractions.Model;
using Manager.Abstractions.Services;
using Manager.Service.Entities;
using Manager.Service.Extentions;
using Microsoft.Extensions.Logging;
using MongoDB.Entities;

namespace Manager.Service.Storages;

public class WorkerTaskPersistentStorage : MongoDBPersistentStorage<string, List<IWorkerTask>, WorkerTaskListEntity>, ITaskStorage
{

    public WorkerTaskPersistentStorage(ILogger<MongoDBPersistentStorage<string, List<IWorkerTask>, WorkerTaskListEntity>> logger, DB db)
        : this((k, tl) => tl.ToEntity(k), logger, (k, el) => el.WorkerTasks.Select(e => e.ToModel(k)).ToList(), db)
    {

    }

    private WorkerTaskPersistentStorage(Func<string, List<IWorkerTask>, WorkerTaskListEntity> toEntity, 
        ILogger<MongoDBPersistentStorage<string, List<IWorkerTask>, WorkerTaskListEntity>> logger, 
        Func<string, WorkerTaskListEntity, List<IWorkerTask>> toValue, DB db)
        : base(toEntity, logger, toValue, db)
    {
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
