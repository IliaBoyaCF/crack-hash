using Contracts.ManagerToWorker;

namespace Manager.Abstractions.Services;

public interface IRequestFinalizer
{
    Task ProcessWorkerResponseAsync(CrackHashWorkerResponse response);
}
