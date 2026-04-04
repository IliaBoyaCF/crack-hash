using Contracts.WorkerToManager;
using Refit;

namespace Contracts.ManagerToWorker;

public interface IWorkerApi
{
    [Post("/internal/api/worker/hash/crack/task")]
    public Task<IApiResponse> AssignTask([Body] CrackHashManagerRequest request);

    [Get("/internal/api/worker/hash/crack/ping")]
    public Task<IApiResponse> Ping();

    [Get("/internal/api/worker/hash/crack/progress")]
    public Task<ApiResponse<TaskStatusResponse>> Progress();
}
