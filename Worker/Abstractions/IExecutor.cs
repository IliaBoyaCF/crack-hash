using Contracts.ManagerToWorker;

namespace Worker.Abstractions;

public interface IExecutor
{

    CrackHashManagerRequest? TaskBeingExecuted { get; }

    float CurrentTaskProgress { get; }

    Task<CrackHashWorkerResponse> Execute(CrackHashManagerRequest request);
}
