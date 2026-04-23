using Manager.Abstractions.Model;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Manager.Service.Services;

public class RequestRecovery : IRequestRecovery
{

    private readonly ILogger<RequestRecovery> _logger;
    
    private readonly IRequestQueue _requestQueue;
    private readonly IRequestStorage _requestStorage;

    public RequestRecovery(IRequestStorage requestStorage, IRequestQueue requestQueue, ILogger<RequestRecovery> logger)
    {
        _requestStorage = requestStorage;
        _requestQueue = requestQueue;
        _logger = logger;
    }

    public void Recover()
    {
        Task.Run(RecoverAsync).Wait();
    }

    public async Task RecoverAsync()
    {
        _logger.LogInformation("Checking saved unprocessed requests.");
        var pendingRequests = await _requestStorage.GetByStatusesAsync([RequestStatus.PENDING]);
        if (pendingRequests.Any())
        {
            _logger.LogInformation("Found {Count} unprocessed requests. Filling request queue.", pendingRequests.Count);
        }
        foreach (var pendingRequest in pendingRequests)
        {
            if (_requestQueue.Count == _requestQueue.Capacity)
            {
                _logger.LogError("Can't fully recover requests due to request queue insufficient capacity.");
                throw new OverflowException("Request queue insufficient capacity");
            }
            await _requestQueue.EnqueueAsync(pendingRequest);
        }
        _logger.LogInformation("Finished recovery.");
    }
}
