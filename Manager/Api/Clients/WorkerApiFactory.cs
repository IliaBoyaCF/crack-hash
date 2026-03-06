using Contracts.ManagerToWorker;
using Refit;
using System.Collections.Concurrent;

namespace Manager.Api.Clients;

public class WorkerApiFactory : IWorkerApiFactory
{

    private readonly ConcurrentDictionary<Uri, IWorkerApi> _clients = [];

    private readonly IHttpClientFactory _httpClientFactory;

    public WorkerApiFactory(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public IWorkerApi CreateWorkerApi(Uri baseUri)
    {
        return _clients.GetOrAdd(baseUri, address =>
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = baseUri;

            return RestService.For<IWorkerApi>(httpClient);
        });
    }
}
