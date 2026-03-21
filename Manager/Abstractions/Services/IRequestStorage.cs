using Manager.Abstractions.Model;

namespace Manager.Abstractions.Services;

public interface IRequestStorage : IStorage<string, IRequestInfo>
{
    Task<IReadOnlyCollection<IRequestInfo>> GetByStatusesAsync(IEnumerable<RequestStatus> statuses, CancellationToken cancellationToken = default);
}
