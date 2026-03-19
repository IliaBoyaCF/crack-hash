using Contracts.ManagerToWorker;
using Manager.Abstractions.Model;
using Manager.Abstractions.Options;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Options;

namespace Manager.Service.Services;

public class WorkerMonitor : IWorkerMonitor
{

    private IWorkerApiFactory _workerApiFactory;
    private readonly IOptions<WorkerOptions> _workerOptions;

    public WorkerMonitor(IOptions<WorkerOptions> workerOptions, IWorkerApiFactory workerApiFactory)
    {
        _workerOptions = workerOptions;
        _workerApiFactory = workerApiFactory;
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
}
