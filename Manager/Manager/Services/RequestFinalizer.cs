using Contracts.ManagerToWorker;
using Manager.Abstractions.Model;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Manager.Service.Services;

public class RequestFinalizer : IRequestFinalizer
{

    private readonly IRequestStorage _requestStorage;
    private readonly ITaskStorage _taskStorage;

    private readonly ILogger<RequestFinalizer> _logger;

    public RequestFinalizer(ITaskStorage taskStorage, IRequestStorage requestStorage, ILogger<RequestFinalizer> logger)
    {
        _taskStorage = taskStorage;
        _requestStorage = requestStorage;
        _logger = logger;
    }

    public async Task ProcessWorkerResponse(CrackHashWorkerResponse response)
    {

        _logger.LogInformation("Got response from worker for request {response.RequestId}", response.RequestId);

        var relatedTasks = await _taskStorage.GetAsync(response.RequestId);

        bool allTasksCompleted = true;

        foreach (var task in relatedTasks)
        {

            if (task.Request.PartNumber == response.PartNumber)
            {
                task.Status = RequestStatus.READY;
            }

            if (task.Status != RequestStatus.READY)
            {
                allTasksCompleted = false;
            }
        }

        await _taskStorage.UpsertAsync(response.RequestId, relatedTasks);

        _logger.LogInformation("Task ({response.RequestId}, {response.PartNumber}) marked as READY", response.RequestId, response.PartNumber);

        var request = await _requestStorage.GetAsync(response.RequestId);
        request.AddResults(response.Answers);

        request.Status = request.Status switch
        {
            RequestStatus.ERROR => RequestStatus.READY_WITH_FAULTS,
            RequestStatus.READY_WITH_FAULTS => RequestStatus.READY_WITH_FAULTS,
            RequestStatus.READY => RequestStatus.READY,
            _ => RequestStatus.IN_PROGRESS_PARTIAL_READY,
        };

        _logger.LogInformation("Answers from task ({response.RequestId}, {response.PartNumber}) have been added to request {response.RequestId}", response.RequestId, response.PartNumber, response.RequestId);

        if (allTasksCompleted)
        {
            request.Status = RequestStatus.READY;
            _logger.LogInformation("All tasks are READY for request {response.RequestId} so it marked as READY", response.RequestId);
        }

        await _requestStorage.UpsertAsync(response.RequestId, request);
        
    }
}
