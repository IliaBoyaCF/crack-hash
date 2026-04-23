using Contracts.ManagerToWorker;
using Contracts.WorkerToManager;
using DnsClient.Internal;
using Manager.Abstractions.Model;
using Manager.Abstractions.Options;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Manager.Service.Services;

public class WorkerMonitor : IWorkerMonitor
{

    private IWorkerApiFactory _workerApiFactory;
    private readonly IOptions<WorkerOptions> _workerOptions;

    private readonly ILogger<WorkerMonitor> _logger;

    public WorkerMonitor(IOptions<WorkerOptions> workerOptions, IWorkerApiFactory workerApiFactory, ILogger<WorkerMonitor> logger)
    {
        _workerOptions = workerOptions;
        _workerApiFactory = workerApiFactory;
        _logger = logger;
    }

    public async Task<List<WorkerDescription>> GetLiveWorkersAsync()
    {

        var tasks = _workerOptions.Value.Instances
            .Select(
                async worker =>
                {
                    try
                    {
                        var workerApi = _workerApiFactory.CreateWorkerApi(worker.Uri);
                        var response = await workerApi.Ping();

                        return response.StatusCode == System.Net.HttpStatusCode.OK ? worker : null;
                    }
                    catch
                    {
                        return null;
                    }  
                }
            );

        var results = await Task.WhenAll(tasks);

        return results.Where(w => w != null).ToList()!;

    }

    public async Task<bool> IsAliveAsync(WorkerDescription workerDescription)
    {
        var workerApi = _workerApiFactory.CreateWorkerApi(workerDescription.Uri);
        try
        {
            var response = await workerApi.Ping();
            return response.StatusCode == System.Net.HttpStatusCode.OK;
        }
        catch
        {
            return false;
        }

    }

    public async Task<TaskStatusResponse?> GetTaskProgressAsync(WorkerDescription workerDescription)
    {
        var workerApi = _workerApiFactory.CreateWorkerApi(workerDescription.Uri);

        try
        {
            var response = await workerApi.Progress();
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            _logger.LogInformation("Got worker progress response with content {Content}", response.Content);
            return response.Content;
        }
        catch
        {
            return null;
        }

    }
}
