using Contracts.WorkerToManager;
using Manager.Abstractions.Model;

namespace Manager.Abstractions.Services;

public interface IWorkerMonitor
{
    Task<List<WorkerDescription>> GetLiveWorkersAsync();

    Task<bool> IsAliveAsync(WorkerDescription workerDescription);

    Task<TaskStatusResponse?> GetTaskProgressAsync(WorkerDescription workerDescription);
}
