using Manager.Abstractions.Events;
using Manager.Abstractions.Model;
using Manager.Abstractions.Services;
using Manager.Service.Model;
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
    private readonly IEventBus _eventBus;

    private readonly IDisposable _requestCompetionSubscribtion;
    private readonly IDisposable _requestTimeoutSubscribtion;

    private readonly ManualResetEventSlim _requestCompleted = new ManualResetEventSlim(true);

    public RequestConsumer(IRequestQueue requestQueue, ITaskScheduler taskScheduler, ILogger<RequestConsumer> logger, IPlanner planner, ICrackedHashCache cache, IRequestStorage requestStorage, ITimeoutMonitor<string> timeoutMonitor, IEventBus eventBus)
    {
        _requestQueue = requestQueue;
        _taskScheduler = taskScheduler;
        _logger = logger;
        _planner = planner;
        _cache = cache;
        _requestStorage = requestStorage;
        _timeoutMonitor = timeoutMonitor;
        _eventBus = eventBus;
        _requestCompetionSubscribtion = _eventBus.Subscribe<RequestCompletionEvent>(OnRequestCompleted);
        _requestTimeoutSubscribtion = _eventBus.Subscribe<TimeoutEvent>(OnRequestTimeout);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Request consumer started work.");
        await foreach (var request in _requestQueue.DequeueAllAsync(stoppingToken))
        {

            _requestCompleted.Wait(stoppingToken);
            _requestCompleted.Reset();

            if (request.Status != RequestStatus.PENDING)
            {
                _requestCompleted.Set();
                continue;
            }


            var now = DateTime.UtcNow;

            if (request.IsTimeoutEnabled && now - request.StartedAt >= request.TimeoutInterval)
            {
                await OnUnprocessedRequestTimeout(request);
                continue;
            }
            _logger.LogInformation("Checking for cached value with cache key {cacheKey}", (request.CrackRequest.Hash, request.CrackRequest.MaxLength));

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

        _timeoutMonitor.TryAdd(request.Id.ToString(), request, resetStartedAt: true);
        request.Status = RequestStatus.IN_PROGRESS;

        await _requestStorage.UpsertAsync(request.Key, request);

        _logger.LogInformation("Tasks assigned for workers for request {request.Id}.", request.Id);
    }

    private async Task OnPrecomputedValueFound(IRequestInfo request, IEnumerable<string> precomputed)
    {
        _logger.LogInformation("Found precomputed answers {Answeres} for hash {Hash} in cache. Setting answers without scheduling to compution.", precomputed, request.CrackRequest.Hash);
        request.AddResults(precomputed);
        request.Status = RequestStatus.READY;
        await _requestStorage.UpsertAsync(request.Key, request);
        _eventBus.Publish(new RequestCompletionEvent { Source = request });
        _requestCompleted.Set();
    }

    private async Task OnUnprocessedRequestTimeout(IRequestInfo request)
    {
        var actualRequest = await _requestStorage.GetAsync(request.Key);
        actualRequest.OnTimeout();
        await _requestStorage.UpsertAsync(actualRequest.Key, actualRequest);
        _requestCompleted.Set();
    }

    private void OnRequestCompleted(RequestCompletionEvent @event) 
    {
        if (@event.CompletedFromCache)
        {
            return;
        }
        _logger.LogInformation("Request {requestId} completed. Trying to cache results (cache key {cacheKey}).", @event.Source.Id, (@event.Source.CrackRequest.Hash, @event.Source.CrackRequest.MaxLength));
        _timeoutMonitor.TryRemove(@event.Source.Key);
        var cached = _cache.TryAdd(@event.Source.CrackRequest.Hash, @event.Source.CrackRequest.MaxLength, @event.Source.Data!);
        _logger.LogInformation("{Status} to cache results for request {requestId}", cached ? "Successfull attempt" : "Failed attempt", @event.Source.Id);
        _requestCompleted.Set();
    }

    private void OnRequestTimeout(TimeoutEvent @event)
    {
        var source = @event.Source;
        if (source is RequestInfo requestInfo)
        {
            _logger.LogInformation("Start processing next queued request due to {requestId} request is timeouted.", requestInfo.Id);
            _requestCompleted.Set();
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _requestCompetionSubscribtion.Dispose();
        _requestTimeoutSubscribtion.Dispose();
    }
}
