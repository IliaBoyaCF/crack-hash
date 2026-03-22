using Contracts.ManagerToWorker;
using Microsoft.Extensions.Logging;
using Worker.Abstractions;

namespace Worker.Service
{
    public class Scheduler : IWorker
    {

        private readonly ILogger<Scheduler> _logger;

        private readonly IFinalizer _finalizer;
        private IExecutor _executor;

        public Scheduler(IFinalizer finalizer, IExecutor executor, ILogger<Scheduler> logger)
        {
            _finalizer = finalizer; _executor = executor;
            _logger = logger;
        }

        public Task Schedule(CrackHashManagerRequest request)
        {

            _logger.LogInformation("Got request: {RequestId}, {PartNumber}/{PartCount}", request.RequestId, request.PartNumber, request.PartCount);

            var task = Task.Factory.StartNew(           
                () => 
                {
                    var task = _executor.Execute(request).ContinueWith(t => _finalizer.CompleteRequestAsync(t.Result)).Unwrap();
                    task.Wait();
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            );

            _logger.LogInformation($"Request was scheduled.");

            return task;
        }
    }
}
