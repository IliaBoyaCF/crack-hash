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

        await _taskStorage.UpsertAsync(requestId, []);

        _logger.LogInformation($"Created record in task storage for {requestId} request.");

        var scheduledTasks = new List<Task>();
        foreach (var task in tasks)
        {
            var workerApi = _workerApiFactory.CreateWorkerApi(task.WorkerAddress);
            var scheduledTask = workerApi.AssignTask(task.Request).ContinueWith(async t =>
            {
                await _taskStorage.UpsertAsync(requestId, [.. await _taskStorage.GetAsync(requestId)]);
                //_taskStorage[requestId].Add(task);
                _timeoutMonitor.TryAdd(task.Request.RequestId, task, resetStartedAt: true);
                
                _logger.LogInformation($"Assigned task for {task.WorkerAddress} for request: {requestId}");
            }).Unwrap();
            scheduledTasks.Add(scheduledTask);
        }

        await Task.WhenAll(scheduledTasks);
    }
}
