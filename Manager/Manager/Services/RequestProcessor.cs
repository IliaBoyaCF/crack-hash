using Manager.Abstractions.Events;
using Manager.Abstractions.Exceptions;
using Manager.Abstractions.Model;
using Manager.Abstractions.Options;
using Manager.Abstractions.Services;
using Manager.Service.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Manager.Service.Services;

public class RequestProcessor : IManager, IDisposable
{

    private readonly IRequestStorage _requestStorage;
    private readonly IRequestQueue _requestQueue;
    private readonly ICrackedHashCache _cache;
    private readonly IOptions<TimeoutOptions> _timeoutOptions;
    private readonly IEventBus _eventBus;

    private readonly IDisposable _requestTimeoutSubscribtion;

    private readonly ILogger<RequestProcessor> _logger;

    public RequestProcessor(IOptions<TimeoutOptions> timeoutOptions, IRequestStorage requestStorage, ILogger<RequestProcessor> logger, IRequestQueue requestQueue, ICrackedHashCache cache, IEventBus eventBus)
    {
        _timeoutOptions = timeoutOptions;
        _requestStorage = requestStorage;
        _logger = logger;
        _requestQueue = requestQueue;
        _cache = cache;
        _eventBus = eventBus;
        _requestTimeoutSubscribtion = _eventBus.Subscribe<TimeoutEvent>(OnRequestTimeout);
    }

    public void Dispose()
    {
        _requestTimeoutSubscribtion.Dispose();
    }

    public async Task<IRequestInfo> GetStatusAsync(Guid requestId)
    {
        try
        {
            _logger.LogInformation($"Got status check for request with GUID: {requestId}");
            var requestInfo = await _requestStorage.GetAsync(requestId.ToString());
            return requestInfo;
        }
        catch (KeyNotFoundException e)
        {
            throw new NoSuchElementException($"Request with GUID {requestId} is unknown.", e);
        }

    }

    public async Task<Guid> RegisterAsync(CrackRequest request)
    {
        _logger.LogInformation($"Got crack request for hash: {request.Hash} and max length {request.MaxLength}");
        Guid requestId = Guid.NewGuid();
        string requestIdStr = requestId.ToString();

        var savedRequest = new RequestInfo 
        { 
            Id = requestId,
            CrackRequest = request,
            TimeoutInterval = _timeoutOptions.Value.RequestTimeout, 
            Status = RequestStatus.IN_PROGRESS 
        };

        //savedRequest.Timeout += OnRequestTimeout;
        //savedRequest.Completed += OnRequestCompleted;

        await _requestStorage.UpsertAsync(requestIdStr, savedRequest);
        if (_cache.TryGetCached(request.Hash, request.MaxLength, out IEnumerable<string>? answers))
        {
            _logger.LogInformation("Found precomputed answers for hash {Hash} in cache. Setting answers without scheduling to compution.", request.Hash);
            savedRequest.Status = RequestStatus.READY;
            savedRequest.AddResults(answers!);
        }
        else
        {
            await _requestQueue.EnqueueAsync(savedRequest);
        }

        _logger.LogInformation("Assigned GUID: {request.Id} for request and saved it.", savedRequest.Id);

        return requestId;

    }

    private void OnRequestCompleted(object? sender, EventArgs e)
    {
        if (sender is RequestInfo requestInfo)
        {
            requestInfo.Timeout -= OnRequestTimeout;
            requestInfo.Completed -= OnRequestCompleted;
        }
    }

    private void OnRequestTimeout(object? sender, EventArgs e)
    {
        if (sender is RequestInfo requestInfo)
        {
            OnRequestTimeout(requestInfo);
        }
    }

    private void OnRequestTimeout(IRequestInfo requestInfo)
    {
        switch (requestInfo.Status)
        {
            case RequestStatus.READY_WITH_FAULTS:
            case RequestStatus.IN_PROGRESS_PARTIAL_READY:
                _logger.LogInformation("Marking request as {RequestStatus} due to timeout and partial success.", RequestStatus.READY_WITH_FAULTS);
                requestInfo.Status = RequestStatus.READY_WITH_FAULTS;
                break;
            case RequestStatus.READY:
                break;
            default:
                _logger.LogInformation($"Marking request as ERROR due to timeout");
                requestInfo.Status = RequestStatus.ERROR;
                break;
        }
        //requestInfo.Timeout -= OnRequestTimeout;
    }
    private void OnRequestTimeout(TimeoutEvent @event)
    {
        if (@event.Source is IRequestInfo requestInfo)
        {
            switch (requestInfo.Status)
            {
                case RequestStatus.READY_WITH_FAULTS:
                case RequestStatus.IN_PROGRESS_PARTIAL_READY:
                    _logger.LogInformation("Marking request as {RequestStatus} due to timeout and partial success.", RequestStatus.READY_WITH_FAULTS);
                    requestInfo.Status = RequestStatus.READY_WITH_FAULTS;
                    break;
                case RequestStatus.READY:
                    break;
                default:
                    _logger.LogInformation($"Marking request as ERROR due to timeout");
                    requestInfo.Status = RequestStatus.ERROR;
                    break;
            }
            Task.Run(() => _requestStorage.UpsertAsync(requestInfo.Id.ToString(), requestInfo));
        }
    }

}
