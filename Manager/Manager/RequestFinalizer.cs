using Contracts.ManagerToWorker;
using Manager.Abstractions.Model;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Manager.Service;

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

        _logger.LogInformation($"Got response from worker for request {response.RequestId}");

        var relatedTasks = _taskStorage[response.RequestId];

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

        _taskStorage[response.RequestId] = relatedTasks;

        _logger.LogInformation($"Task ({response.RequestId}, {response.PartNumber}) marked as READY");

        var request = _requestStorage[response.RequestId];
        request.AddResults(response.Answers);

        _logger.LogInformation($"Answers from task ({response.RequestId}, {response.PartNumber}) have been added to request {response.RequestId}");

        if (allTasksCompleted)
        {
            request.Status = RequestStatus.READY;
            _logger.LogInformation($"All tasks are READY for request {response.RequestId} so it marked as READY");
        }

        _requestStorage[response.RequestId] = request;
        
    }
}
