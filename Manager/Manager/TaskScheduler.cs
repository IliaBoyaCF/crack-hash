using Contracts.ManagerToWorker;
using Manager.Abstractions.Model;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Manager.Service;

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

        _taskStorage[requestId] = [];

        _logger.LogInformation($"Created record in task storage for {requestId} request.");

        var scheduledTasks = new List<Task>();
        foreach (var task in tasks)
        {
            var workerApi = _workerApiFactory.CreateWorkerApi(task.WorkerAddress);
            var scheduledTask = workerApi.AssignTask(task.Request).ContinueWith(async t =>
            {
                _taskStorage[requestId].Add(task);
                _timeoutMonitor.TryAdd(task.Request.RequestId, task, resetStartedAt: true);
            });
            scheduledTasks.Add(scheduledTask);
            _logger.LogInformation($"Assigned task for {task.WorkerAddress} for request: {requestId}");
        }

        await Task.WhenAll(scheduledTasks);
    }
}
