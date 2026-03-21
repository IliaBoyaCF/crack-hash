using Manager.Abstractions.Model;
using Manager.Abstractions.Services;

namespace Manager.Service.Storages;

public class RequestStorage : InMemoryStorage<string, IRequestInfo>, IRequestStorage
{
    public async Task<IReadOnlyCollection<IRequestInfo>> GetByStatusesAsync(IEnumerable<RequestStatus> statuses, CancellationToken cancellationToken = default)
    {
        var res = new List<IRequestInfo>();

        await foreach (var request in GetAllAsyncEnumerable())
        {
            if (statuses.Contains(request.Status))
            {
                res.Add(request);
            }
        }

        return res;

    }
}
