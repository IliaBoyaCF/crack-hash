using Manager.Abstractions.Model;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Manager.Service;

public class RequestConsumer : BackgroundService, IRequestConsumer
{

    private readonly ICrackedHashCache _cache;
    private readonly IRequestQueue _requestQueue;
    private readonly IRequestStorage _requestStorage;
    private readonly ITaskScheduler _taskScheduler;
    private readonly ILogger<RequestConsumer> _logger;
    private readonly IPlanner _planner;

    private readonly ManualResetEventSlim _requestCompleted = new ManualResetEventSlim(true);

    public RequestConsumer(IRequestQueue requestQueue, ITaskScheduler taskScheduler, ILogger<RequestConsumer> logger, IPlanner planner, ICrackedHashCache cache, IRequestStorage requestStorage)
    {
        _requestQueue = requestQueue;
        _taskScheduler = taskScheduler;
        _logger = logger;
        _planner = planner;
        _cache = cache;
        _requestStorage = requestStorage;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Request consumer started work.");
        await foreach (var request in _requestQueue.DequeueAllAsync(stoppingToken))
        {

            _requestCompleted.Wait(stoppingToken);
            _requestCompleted.Reset();

            if (_cache.TryGetCached(request.CrackRequest.Hash, out IEnumerable<string>? precomputed))
            {
                _logger.LogInformation("Found precomputed answers for hash {Hash} in cache. Setting answers without scheduling to compution.", request.CrackRequest.Hash);
                request.AddResults(precomputed!);
                request.Status = RequestStatus.READY;
                _requestStorage[request.Id.ToString()] = request;
                _requestCompleted.Set();
                continue;
            }

            _logger.LogInformation("Get request {request.Id} from queue.", request.Id);

            var tasks = await _planner.CreateWorkerTasksAsync(request.Id.ToString(), request.CrackRequest);

            _logger.LogInformation("Created tasks for workers for request {request.Id}.", request.Id);

            await _taskScheduler.ScheduleAsync(tasks);

            request.Completed += OnRequestCompleted;
            
            request.StartTimoutMonitoring();

            _logger.LogInformation("Tasks assigned for workers for request {request.Id}.", request.Id);

        }
    }

    private void OnRequestCompleted(object? sender, EventArgs e)
    {
        _requestCompleted.Set();
        if (sender is IRequestInfo requestInfo)
        {
            if (requestInfo.Status != RequestStatus.READY)
            {
                return;
            }
            _cache.TryAdd(requestInfo.CrackRequest.Hash, requestInfo.Data!);
        }
    }
}
