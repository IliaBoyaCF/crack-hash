using Manager.Abstractions.Model;
using Manager.Service.Entities;
using Manager.Service.Model;

namespace Manager.Service.Extentions;

public static class RequestInfoEntityExtentions
{
    public static IRequestInfo ToModel(this RequestInfoEntity requestInfoEntity, string id)
    {
        return new RequestInfo
        {
            Id = Guid.Parse(requestInfoEntity.ID),
            CrackRequest = new CrackRequest
            {
                Hash = requestInfoEntity.Hash,
                MaxLength = requestInfoEntity.MaxLength,
            },
            Status = requestInfoEntity.Status,
            Data = requestInfoEntity.Data,
            TimeoutInterval = requestInfoEntity.TimeoutInterval,
            IsTimeoutEnabled = requestInfoEntity.IsTimeoutEnabled,
            StartedAt = requestInfoEntity.StartedAt,
        };
    }
}
