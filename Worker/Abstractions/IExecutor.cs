using Contracts.ManagerToWorker;

namespace Worker.Abstractions
{
    public interface IExecutor
    {
        Task<CrackHashWorkerResponse> Execute(CrackHashManagerRequest request);
    }
}
