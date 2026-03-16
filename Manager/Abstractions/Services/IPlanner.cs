using Manager.Abstractions.Model;

namespace Manager.Abstractions.Services;

public interface IPlanner
{
    Task<IEnumerable<IWorkerTask>> CreateWorkerTasksAsync(string requestId, CrackRequest request);
}
