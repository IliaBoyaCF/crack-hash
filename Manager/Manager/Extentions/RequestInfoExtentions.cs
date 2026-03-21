using Manager.Abstractions.Model;
using Manager.Service.Entities;

namespace Manager.Service.Extentions;

public static class RequestInfoExtentions
{
    public static RequestInfoEntity ToEntity(this IRequestInfo requestInfo, string id)
    {
        return new RequestInfoEntity
        {
            ID = requestInfo.Id.ToString(),
            Hash = requestInfo.CrackRequest.Hash,
            MaxLength = requestInfo.CrackRequest.MaxLength,
            Status = requestInfo.Status,
            Data = requestInfo.Data?.ToList(),
            IsTimeoutEnabled = requestInfo.IsTimeoutEnabled,
            TimeoutInterval = requestInfo.TimeoutInterval,
            StartedAt = requestInfo.StartedAt
        };
    }
}
