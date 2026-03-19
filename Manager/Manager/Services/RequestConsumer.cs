using Manager.Abstractions.Model;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Manager.Service.Services;

public class RequestConsumer : BackgroundService, IRequestConsumer
{

    private readonly ICrackedHashCache _cache;
    private readonly IRequestQueue _requestQueue;
    private readonly IRequestStorage _requestStorage;
    private readonly ITaskScheduler _taskScheduler;
    private readonly ILogger<RequestConsumer> _logger;
    private readonly IPlanner _planner;
    private readonly ITimeoutMonitor<string> _timeoutMonitor;

    private readonly ManualResetEventSlim _requestCompleted = new ManualResetEventSlim(true);

    public RequestConsumer(IRequestQueue requestQueue, ITaskScheduler taskScheduler, ILogger<RequestConsumer> logger, IPlanner planner, ICrackedHashCache cache, IRequestStorage requestStorage, ITimeoutMonitor<string> timeoutMonitor)
    {
        _requestQueue = requestQueue;
        _taskScheduler = taskScheduler;
        _logger = logger;
        _planner = planner;
        _cache = cache;
        _requestStorage = requestStorage;
        _timeoutMonitor = timeoutMonitor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Request consumer started work.");
        await foreach (var request in _requestQueue.DequeueAllAsync(stoppingToken))
        {

            _requestCompleted.Wait(stoppingToken);
            _requestCompleted.Reset();

            var now = DateTime.UtcNow;

            if (request.IsTimeoutEnabled && now - request.StartedAt >= request.TimeoutInterval)
            {
                OnUnprocessedRequestTimeout(request);
                continue;
            }

            if (_cache.TryGetCached(request.CrackRequest.Hash, request.CrackRequest.MaxLength, out IEnumerable<string>? precomputed))
            {
                await OnPrecomputedValueFound(request, precomputed!);
                continue;
            }

            await ScheduleExecution(request);

        }
    }

    private async Task ScheduleExecution(IRequestInfo request)
    {
        _logger.LogInformation("Get request {request.Id} from queue.", request.Id);

        var tasks = await _planner.CreateWorkerTasksAsync(request.Id.ToString(), request.CrackRequest);

        _logger.LogInformation("Created tasks for workers for request {request.Id}.", request.Id);

        await _taskScheduler.ScheduleAsync(tasks);

        request.Completed += OnRequestCompleted;

        _timeoutMonitor.TryAdd(request.Id.ToString(), request, resetStartedAt: true);

        _logger.LogInformation("Tasks assigned for workers for request {request.Id}.", request.Id);
    }

    private async Task OnPrecomputedValueFound(IRequestInfo request, IEnumerable<string> precomputed)
    {
        _logger.LogInformation("Found precomputed answers {Answeres} for hash {Hash} in cache. Setting answers without scheduling to compution.", precomputed, request.CrackRequest.Hash);
        request.AddResults(precomputed);
        request.Status = RequestStatus.READY;
        await _requestStorage.UpsertAsync(request.Id.ToString(), request);
        _requestCompleted.Set();
    }

    private void OnUnprocessedRequestTimeout(IRequestInfo request)
    {
        _requestCompleted.Set();
        request.Timeout += OnRequestCompleted;
        request.OnTimeout();
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
            _cache.TryAdd(requestInfo.CrackRequest.Hash, requestInfo.CrackRequest.MaxLength, requestInfo.Data!);
            requestInfo.Completed -= OnRequestCompleted;
        }
    }
}
