using Manager.Abstractions.Exceptions;
using Manager.Abstractions.Model;
using Manager.Abstractions.Options;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Manager.Service
{
    public class RequestProcessor : IManager
    {

        private readonly IRequestStorage _requestStorage;
        private readonly IOptions<TimeoutOptions> _timeoutOptions;

        private readonly ITaskScheduler _taskScheduler;
        private readonly IPlanner _planner;

        private readonly ILogger<RequestProcessor> _logger;

        public RequestProcessor(IOptions<TimeoutOptions> timeoutOptions, IRequestStorage requestStorage, IPlanner planner, ITaskScheduler taskScheduler, ILogger<RequestProcessor> logger)
        {
            _timeoutOptions = timeoutOptions;
            _requestStorage = requestStorage;
            _planner = planner;
            _taskScheduler = taskScheduler;
            _logger = logger;
        }

        public async Task<IRequestInfo> GetStatusAsync(Guid requestId)
        {
            try
            {
                _logger.LogInformation($"Got status check for request with GUID: {requestId}");
                var requestInfo = _requestStorage[requestId.ToString()];
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

            var savedRequest = new RequestInfo { TimeoutInterval = _timeoutOptions.Value.RequestTimeout, Status = RequestStatus.IN_PROGRESS };
            _requestStorage.Add(requestIdStr, savedRequest);
            savedRequest.Timeout += OnRequestTimeout;
            savedRequest.StartTimoutMonitoring();

            _logger.LogInformation($"Assigned GUID: {requestId} for request and saved it.");

            var tasks = await _planner.CreateWorkerTasksAsync(requestIdStr, request);

            _logger.LogInformation($"Created tasks for workers for request {requestId}.");

            await _taskScheduler.ScheduleAsync(tasks);

            _logger.LogInformation($"Tasks assigned for workers for request {requestId}.");

            return requestId;

        }

        private void OnRequestTimeout(object? sender, EventArgs e)
        {
            if (sender is RequestInfo requestInfo)
            {
                _logger.LogInformation($"Marking request as ERROR due to timeout");
                requestInfo.Status = RequestStatus.ERROR;
                requestInfo.Timeout -= OnRequestTimeout;
            }
        }
    }
}
