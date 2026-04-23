using Worker.Abstractions;
using Contracts.ManagerToWorker;
using Contracts.WorkerToManager;
using Microsoft.Extensions.Logging;

namespace Worker.Service
{
    public class Finalizer : IFinalizer
    {

        private readonly ILogger<Finalizer> _logger;

        private readonly IManagerApi _managerApi;

        public Finalizer(IManagerApi managerApi, ILogger<Finalizer> logger)
        {
            _managerApi = managerApi;
            _logger = logger;
        }

        public async Task CompleteRequestAsync(CrackHashWorkerResponse response)
        {
            await _managerApi.SendResponseAsync(response);

            _logger.LogInformation("Sent response to a manager for request: ({RequestId}, {PartNumber})", response.RequestId, response.PartNumber);
        }
    }
}
