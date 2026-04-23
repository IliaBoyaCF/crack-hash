using Contracts.ManagerToWorker;

namespace Worker.Abstractions;

public interface IExecutor
{

    float CurrentTaskProgress { get; }

    Task<CrackHashWorkerResponse> Execute(CrackHashManagerRequest request);
}
