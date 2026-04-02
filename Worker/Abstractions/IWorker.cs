using Contracts.ManagerToWorker;

namespace Worker.Abstractions;

public interface IWorker
{

    RequestData RequestData { get; }

    Task Schedule(CrackHashManagerRequest request);

    (string requestId, int partNumber, int partCount, float)? TaskProgress();
}

public enum RequestStatus
{
    IN_PROGRESS,
    COMPLETED,
}

public class RequestData
{
    public required CrackHashManagerRequest Request { get; init; }

    public RequestStatus Status { get; set; }

}
