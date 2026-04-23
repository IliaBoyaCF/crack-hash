using Manager.Abstractions.Model;
using Manager.Service.Entities;
using Manager.Service.Model;

namespace Manager.Service.Extentions;

public static class WorkerTaskEntityExtentions
{
    public static IWorkerTask ToModel(this WorkerTaskEntity entity, string key)
    {
        return new WorkerTask
        {
            Request = new Contracts.ManagerToWorker.CrackHashManagerRequest
            {
                RequestId = entity.TaskId.RequestId,
                PartNumber = entity.TaskId.PartNumber,
                Hash = entity.Hash,
                MaxLength = entity.MaxLength,
                Alphabet = entity.Alphabet,
            },
            WorkerAddress = entity.WorkerAddress,
            Status = entity.Status,
            TimeoutInterval = entity.TimeoutInterval,
            IsTimeoutEnabled = entity.IsTimeoutEnabled,
            StartedAt = entity.StartedAt,
        };
    }
}
