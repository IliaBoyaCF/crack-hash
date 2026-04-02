using Contracts.ManagerToWorker;

namespace Worker.Abstractions;

public interface IWorker
{

    Task Schedule(CrackHashManagerRequest request);

    (string requestId, int partNumber, int partCount, float)? TaskProgress();
}
