using Manager.Abstractions.Model;
using Manager.Abstractions.Options;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Manager.Service.Services;

public class Planner : IPlanner
{

    private readonly IWorkerMonitor _workerMonitor;
    private readonly IOptions<TimeoutOptions> _timeoutOptions;

    private readonly ILogger<Planner> _logger;

    private readonly string[] _alphabet;

    public Planner(IOptions<AlphabetOptions> alphabet, IWorkerMonitor workerMonitor, IOptions<TimeoutOptions> timeoutOptions, ILogger<Planner> logger)
    {
        _workerMonitor = workerMonitor;
        _alphabet = alphabet.Value.Symbols.ToArray();
        Array.Sort(_alphabet);
        _timeoutOptions = timeoutOptions;
        _logger = logger;
    }

    public async Task<IEnumerable<IWorkerTask>> CreateWorkerTasksAsync(string requestId, CrackRequest request)
    {

        _logger.LogInformation($"Getting all alive workers");

        var aviliableWorkers = await _workerMonitor.GetLiveWorkersAsync();

        int partCount = aviliableWorkers.Count;

        _logger.LogInformation($"Found {partCount} alive workers. Creating tasks.");

        var tasks = new List<IWorkerTask>();

        int partNumber = 0;

        foreach (var worker in aviliableWorkers)
        {
            var task = new WorkerTask
            {
                Request = new Contracts.ManagerToWorker.CrackHashManagerRequest
                {
                    RequestId = requestId,
                    PartCount = partCount,
                    PartNumber = partNumber,
                    Hash = request.Hash,
                    MaxLength = request.MaxLength,
                    Alphabet = _alphabet,
                },
                Status = RequestStatus.IN_PROGRESS,
                WorkerAddress = worker.Uri,
                TimeoutInterval = _timeoutOptions.Value.WorkerTaskTimeout,
            };
            tasks.Add(task);
            partNumber++;
        }

        return tasks;

    }
}
