using Contracts.ManagerToWorker;
using Manager.Abstractions.Model;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Manager.Service;

public class TaskScheduler : ITaskScheduler
{

    private readonly IWorkerApiFactory _workerApiFactory;
    private readonly ITaskStorage _taskStorage;

    private readonly ILogger<TaskScheduler> _logger;


    public TaskScheduler(IWorkerApiFactory workerApiFactory, ITaskStorage taskStorage, ILogger<TaskScheduler> logger)
    {
        _workerApiFactory = workerApiFactory;
        _taskStorage = taskStorage;
        _logger = logger;
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
                task.StartTimeoutMonitoring();
            });
            scheduledTasks.Add(scheduledTask);
            _logger.LogInformation($"Assigned task for {task.WorkerAddress} for request: {requestId}");
        }

        await Task.WhenAll(scheduledTasks);
    }
}
