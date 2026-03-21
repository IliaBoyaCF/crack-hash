using Contracts.ManagerToWorker;
using Manager.Abstractions.Model;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Manager.Service.Services;

public class TaskScheduler : ITaskScheduler
{

    private readonly IWorkerApiFactory _workerApiFactory;
    private readonly ITaskStorage _taskStorage;
    private readonly ITimeoutMonitor<string> _timeoutMonitor;

    private readonly ILogger<TaskScheduler> _logger;

    public TaskScheduler(IWorkerApiFactory workerApiFactory, ITaskStorage taskStorage, ILogger<TaskScheduler> logger, ITimeoutMonitor<string> timeoutMonitor)
    {
        _workerApiFactory = workerApiFactory;
        _taskStorage = taskStorage;
        _logger = logger;
        _timeoutMonitor = timeoutMonitor;
    }

    public async Task ScheduleAsync(IEnumerable<IWorkerTask> tasks)
    {
        if (!tasks.Any())
        {
            return;
        }

        string requestId = tasks.First().Request.RequestId;

        foreach (var task in tasks)
        {
            _timeoutMonitor.TryAdd(task.Key, task, resetStartedAt: true);
        }

        await _taskStorage.UpsertAsync(requestId, [.. tasks]);

        _logger.LogInformation($"Created record in task storage for {requestId} request.");

        foreach (var task in tasks)
        {
            var workerApi = _workerApiFactory.CreateWorkerApi(task.WorkerAddress);
            await workerApi.AssignTask(task.Request).ContinueWith(t =>
            {
                _logger.LogInformation($"Assigned task for {task.WorkerAddress} for request: {requestId}");
            });
        }

        _logger.LogInformation("Record in task storage for {requestId} now contains [{elements}]", requestId, string.Join(", ", (await _taskStorage.GetAsync(requestId)).Select(t => t.Key)));
    }
}
