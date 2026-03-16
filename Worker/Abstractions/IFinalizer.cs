using Contracts.ManagerToWorker;

namespace Worker.Abstractions
{
    public interface IFinalizer
    {
        Task CompleteRequest(CrackHashWorkerResponse response);
    }
}
