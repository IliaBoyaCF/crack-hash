using Contracts.ManagerToWorker;

namespace Manager.Abstractions.Model;

public interface IWorkerTask
{

    event EventHandler? Timeout;

    TimeSpan TimeoutInterval { get; init; }
    CrackHashManagerRequest Request { get; init; }
    Uri WorkerAddress { get; init; }
    RequestStatus Status { get; set; }

    void StartTimeoutMonitoring();

}
