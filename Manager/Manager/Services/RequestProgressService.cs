using DnsClient.Internal;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Manager.Service.Services;

public class RequestProgressService : IRequestProgressService
{

    private readonly IWorkerMonitor _workerMonitor;
    private readonly IRequestStorage _requestStorage;
    private readonly ITaskStorage _taskStorage;

    private readonly ILogger<RequestProgressService> _logger;

    public RequestProgressService(IWorkerMonitor workerMonitor, IRequestStorage requestStorage, ITaskStorage taskStorage, ILogger<RequestProgressService> logger)
    {
        _workerMonitor = workerMonitor;
        _requestStorage = requestStorage;
        _taskStorage = taskStorage;
        _logger = logger;
    }

    public async Task<float> GetProgressAsync(Guid requestId)
    {

        _logger.LogInformation("Checking progress for request {RequestId}", requestId);

        var requestInfo = await _requestStorage.GetAsync(requestId.ToString());

        switch (requestInfo.Status)
        {
            case Abstractions.Model.RequestStatus.READY:
            case Abstractions.Model.RequestStatus.READY_WITH_FAULTS:
            case Abstractions.Model.RequestStatus.ERROR:
                _logger.LogInformation("Request {RequestId} is done, so it's progress is 100%", requestId);
                return 1f;

            case Abstractions.Model.RequestStatus.PENDING:
                _logger.LogInformation("Request {RequestId} is pending in queue, so it's progress is 0%", requestId);
                return 0f;

            default:

                _logger.LogInformation("Request {RequestId} is in progress, so it's completion percentage is being calculated.", requestId);

                var tasks = await _taskStorage.GetAsync(requestId.ToString());

                var totalPercentage = 0f;

                foreach (var task in tasks)
                {
                    if (task.Status == Abstractions.Model.RequestStatus.READY)
                    {
                        totalPercentage += 1f;
                    }
                }

                var workers = await _workerMonitor.GetLiveWorkersAsync();

                foreach (var worker in workers)
                {
                    var taskStatus = await _workerMonitor.GetTaskProgressAsync(worker);
                    _logger.LogInformation("Got worker progress response for {RequestId} with progress {Progress}.", requestId, taskStatus?.Progress);
                    if (taskStatus == null)
                    {
                        continue;
                    }
                    if (taskStatus.RequestId != requestId.ToString())
                    {
                        continue;
                    }
                    totalPercentage += taskStatus.Progress;
                }

                return totalPercentage / tasks.Count;


        }

    }
}
