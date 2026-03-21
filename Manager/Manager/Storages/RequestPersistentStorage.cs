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
}
