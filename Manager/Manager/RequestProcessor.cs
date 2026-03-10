using Manager.Abstractions.Exceptions;
using Manager.Abstractions.Model;
using Manager.Abstractions.Options;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Manager.Service;

public class RequestProcessor : IManager
{

    private readonly IRequestStorage _requestStorage;
    private readonly IRequestQueue _requestQueue;
    private readonly ICrackedHashCache _cache;
    private readonly IOptions<TimeoutOptions> _timeoutOptions;

    private readonly ILogger<RequestProcessor> _logger;

    public RequestProcessor(IOptions<TimeoutOptions> timeoutOptions, IRequestStorage requestStorage, ILogger<RequestProcessor> logger, IRequestQueue requestQueue, ICrackedHashCache cache)
    {
        _timeoutOptions = timeoutOptions;
        _requestStorage = requestStorage;
        _logger = logger;
        _requestQueue = requestQueue;
        _cache = cache;
    }

    public async Task<IRequestInfo> GetStatusAsync(Guid requestId)
    {
        try
        {
            _logger.LogInformation($"Got status check for request with GUID: {requestId}");
            var requestInfo = _requestStorage[requestId.ToString()];
            var now = DateTime.Now;
            if (IsRequestTimedout(requestInfo, now))
            {
                OnRequestTimeout(requestInfo);
            }
            return requestInfo;
        }
        catch (KeyNotFoundException e)
        {
            throw new NoSuchElementException($"Request with GUID {requestId} is unknown.", e);
        }

    }

    private static bool IsRequestTimedout(IRequestInfo requestInfo, DateTime now)
    {
        return requestInfo.Status != RequestStatus.READY && requestInfo.Status != RequestStatus.READY_WITH_FAULTS && now - requestInfo.CreatedTime > requestInfo.TimeoutInterval;
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

        _requestStorage.Add(requestIdStr, savedRequest);
        if (_cache.TryGetCached(request.Hash, out IEnumerable<string>? answers))
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
                _logger.LogInformation("Marking request as {RequestStatus} due to timeout and partial success.", requestInfo.Status);
                requestInfo.Status = RequestStatus.READY_WITH_FAULTS;
                break;
            case RequestStatus.READY:
                break;
            default:
                _logger.LogInformation($"Marking request as ERROR due to timeout");
                requestInfo.Status = RequestStatus.ERROR;
                break;
        }
        requestInfo.Timeout -= OnRequestTimeout;
    }
}
