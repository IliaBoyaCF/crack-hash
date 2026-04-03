using Contracts.ManagerToWorker;
using Microsoft.Extensions.Logging;
using Worker.Abstractions;

namespace Worker.Service;

public class Scheduler : IWorker
{

    private readonly ILogger<Scheduler> _logger;

    private readonly IFinalizer _finalizer;
    private IExecutor _executor;
    public RequestData RequestData { get; private set; } = new RequestData()
    {
        Request = new CrackHashManagerRequest(),
        Status = RequestStatus.COMPLETED,
    };

    public Scheduler(IFinalizer finalizer, IExecutor executor, ILogger<Scheduler> logger)
    {
        _finalizer = finalizer; _executor = executor;
        _logger = logger;
    }


    public Task Schedule(CrackHashManagerRequest request)
    {

        _logger.LogInformation("Got request: {RequestId}, {PartNumber}/{PartCount}", request.RequestId, request.PartNumber, request.PartCount);

        if (RequestData.Status != RequestStatus.COMPLETED)
        {
            throw new InvalidOperationException("Can't schedule new task until previous is completed.");
        }

        RequestData = new RequestData { Request = request, Status = RequestStatus.IN_PROGRESS, };

        var task = Task.Factory.StartNew(           
            () => 
            {
                var task = _executor
                .Execute(request)
                .ContinueWith(t => _finalizer.CompleteRequestAsync(t.Result))
                .ContinueWith(t => { RequestData.Status = RequestStatus.COMPLETED; return t; })
                .Unwrap();
                task.Wait();
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );

        _logger.LogInformation($"Request was scheduled.");

        return task;
    }

    public (string requestId, int partNumber, int partCount, float)? TaskProgress()
    {
        return (RequestData.Request.RequestId, RequestData.Request.PartNumber, RequestData.Request.PartCount, _executor.CurrentTaskProgress);
    }
}
