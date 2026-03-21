using Manager.Abstractions.Model;
using Manager.Service.Entities;

namespace Manager.Service.Extentions;

public static class WorkerTaskExtentions
{
    public static WorkerTaskEntity ToEntity(this IWorkerTask task, string key)
    {
        var taskId = new WorkerTaskId
        {
            RequestId = task.Request.RequestId,
            PartNumber = task.Request.PartNumber,
            PartCount = task.Request.PartCount,
        };
        return new WorkerTaskEntity
        {
            ID = taskId.ToString(),
            TaskId = taskId,
            MaxLength = task.Request.MaxLength,
            Alphabet = task.Request.Alphabet,
            Hash = task.Request.Hash,
            Status = task.Status,
            WorkerAddress = task.WorkerAddress,
            IsTimeoutEnabled = task.IsTimeoutEnabled,
            TimeoutInterval = task.TimeoutInterval,
            StartedAt = task.StartedAt,
        };
    }
}
