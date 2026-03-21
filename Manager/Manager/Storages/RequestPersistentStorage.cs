using Manager.Abstractions.Model;
using Manager.Abstractions.Services;
using Manager.Service.Entities;
using Manager.Service.Extentions;
using Microsoft.Extensions.Logging;
using MongoDB.Entities;

namespace Manager.Service.Storages;

public class RequestPersistentStorage : MongoDBPersistentStorage<string, IRequestInfo, RequestInfoEntity>, IRequestStorage
{

    public RequestPersistentStorage(ILogger<MongoDBPersistentStorage<string, IRequestInfo, RequestInfoEntity>> logger, DB db) : this((k, v) => v.ToEntity(k), logger, (k, e) => e.ToModel(k), db)
    {

    }

    private RequestPersistentStorage(Func<string, IRequestInfo, RequestInfoEntity> toEntity, ILogger<MongoDBPersistentStorage<string, IRequestInfo, RequestInfoEntity>> logger, Func<string, RequestInfoEntity, IRequestInfo> toValue, DB db) : base(toEntity, logger, toValue, db)
    {
    }

    public async Task<IReadOnlyCollection<IRequestInfo>> GetByStatusesAsync(IEnumerable<RequestStatus> statuses, CancellationToken cancellationToken = default)
    {
        try
        {
            var statusList = statuses.ToList();
            if (!statusList.Any())
            {
                return [];
            }

            var filter = _db.Find<RequestInfoEntity>()
                .Match(e => statusList.Contains(e.Status));

            var entities = await filter.ExecuteAsync(cancellationToken);

            return entities.Select(e => e.ToModel(ParseKey(e.ID))).ToList();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting requests by statuses: {Statuses}", string.Join(", ", statuses));
            throw;
        }
    }
}
