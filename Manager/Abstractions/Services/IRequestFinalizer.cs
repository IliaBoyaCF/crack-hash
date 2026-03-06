using Contracts.ManagerToWorker;

namespace Manager.Abstractions.Services;

public interface IRequestFinalizer
{
    Task ProcessWorkerResponse(CrackHashWorkerResponse response);
}
