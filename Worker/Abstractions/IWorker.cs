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

    public RequestStatus Status {
        get => field;
        set
        {
            field = value;
            if (field == RequestStatus.COMPLETED)
            {
                _completed.Set();
            }
            else
            {
                _completed.Reset();
            }
        }
    } = RequestStatus.IN_PROGRESS;

    private readonly ManualResetEventSlim _completed = new ManualResetEventSlim(false);

    public void WaitForCompetion()
    {
        _completed.Wait();
    }

}
