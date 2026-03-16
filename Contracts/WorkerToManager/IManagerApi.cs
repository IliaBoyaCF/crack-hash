using Contracts.ManagerToWorker;
using Refit;

namespace Contracts.WorkerToManager
{
    public interface IManagerApi
    {
        [Patch("/internal/api/manager/hash/crack/request")]
        Task<IApiResponse> SendResponseAsync([Body] CrackHashWorkerResponse response);
    }
}
