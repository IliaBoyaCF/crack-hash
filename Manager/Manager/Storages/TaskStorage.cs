using Manager.Abstractions.Model;
using Manager.Abstractions.Services;

namespace Manager.Service.Storages;

public class TaskStorage : InMemoryStorage<string, List<IWorkerTask>>, ITaskStorage
{
    public Task UpdateTaskStatusAsync(string requestId, int partNumber, RequestStatus newStatus, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
