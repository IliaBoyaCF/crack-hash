using Contracts.ManagerToWorker;
using Manager.Abstractions.Events;
using Manager.Abstractions.Model;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Manager.Service.Services;

public class RequestFinalizer : IRequestFinalizer
{

    private readonly IRequestStorage _requestStorage;
    private readonly ITaskStorage _taskStorage;
    private readonly IEventBus _eventBus;
    private readonly ITimeoutMonitor<string> _timeoutMonitor;

    private readonly ILogger<RequestFinalizer> _logger;

    public RequestFinalizer(ITaskStorage taskStorage, IRequestStorage requestStorage, ILogger<RequestFinalizer> logger, IEventBus eventBus, ITimeoutMonitor<string> timeoutMonitor)
    {
        _taskStorage = taskStorage;
        _requestStorage = requestStorage;
        _logger = logger;
        _eventBus = eventBus;
        _timeoutMonitor = timeoutMonitor;
    }

    public async Task ProcessWorkerResponse(CrackHashWorkerResponse response)
    {

        _logger.LogInformation("Got response from worker for request {response.RequestId}", response.RequestId);

        var relatedTasks = await _taskStorage.GetAsync(response.RequestId);

        _logger.LogInformation("Related tasks count: {Count}, [{elements}]", relatedTasks.Count, string.Join(", ", relatedTasks.Select(t => t.Key)));

        bool allTasksCompleted = true;

        string completedTaskKey = null;

        foreach (var task in relatedTasks)
        {

            if (task.Request.PartNumber == response.PartNumber)
            {
                task.Status = RequestStatus.READY;
                completedTaskKey = task.Key;
            }

            if (task.Status != RequestStatus.READY)
            {
                allTasksCompleted = false;
            }
        }

        await _taskStorage.UpsertAsync(response.RequestId, relatedTasks);
        if (completedTaskKey == null)
        {
            _logger.LogWarning("Got unknown task with key {key}", completedTaskKey);
            return;
        }
        _timeoutMonitor.TryRemove(completedTaskKey);

        _logger.LogInformation("Task ({response.RequestId}, {response.PartNumber}) marked as READY", response.RequestId, response.PartNumber);

        var request = await _requestStorage.GetAsync(response.RequestId);
        request.AddResults(response.Answers);

        request.Status = request.Status switch
        {
            RequestStatus.ERROR => RequestStatus.READY_WITH_FAULTS,
            RequestStatus.READY_WITH_FAULTS => RequestStatus.READY_WITH_FAULTS,
            RequestStatus.READY => RequestStatus.READY,
            _ => allTasksCompleted ? RequestStatus.READY : RequestStatus.IN_PROGRESS_PARTIAL_READY,
        };

        _logger.LogInformation("Answers from task ({response.RequestId}, {response.PartNumber}) have been added to request {response.RequestId}", response.RequestId, response.PartNumber, response.RequestId);

        await _requestStorage.UpsertAsync(response.RequestId, request);
        
        if (allTasksCompleted)
        {
            _eventBus.Publish(new RequestCompletionEvent() { Source = request });
            _logger.LogInformation("All tasks are READY for request {response.RequestId} so it marked as READY", response.RequestId);
        }

    }
}
