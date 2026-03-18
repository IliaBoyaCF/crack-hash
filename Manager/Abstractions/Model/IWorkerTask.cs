using Contracts.ManagerToWorker;

namespace Manager.Abstractions.Model;

public interface IWorkerTask : ITimeoutable
{

    CrackHashManagerRequest Request { get; init; }
    Uri WorkerAddress { get; init; }
    RequestStatus Status { get; set; }

}
