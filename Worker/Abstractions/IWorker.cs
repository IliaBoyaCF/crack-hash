using Contracts.ManagerToWorker;

namespace Worker.Abstractions
{
    public interface IWorker
    {
        Task Schedule(CrackHashManagerRequest request);
    }
}
