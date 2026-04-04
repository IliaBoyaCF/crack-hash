using Contracts.ManagerToWorker;

namespace Worker.Abstractions
{
    public interface IFinalizer
    {
        Task CompleteRequestAsync(CrackHashWorkerResponse response);
    }
}
