using Manager.Abstractions.Model;

namespace Manager.Abstractions.Services;

public interface ITaskStorage : IStorage<string, List<IWorkerTask>>
{
    public Task UpdateTaskStatusAsync(string requestId, int partNumber, RequestStatus newStatus, CancellationToken cancellationToken = default);
}
