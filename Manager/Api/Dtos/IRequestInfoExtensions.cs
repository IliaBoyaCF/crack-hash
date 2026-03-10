using Manager.Abstractions.Model;

namespace Manager.Api.Dtos;

public static class IRequestInfoExtensions
{
    public static RequestInfoDto ToDto(this IRequestInfo requestInfo)
    {
        var dto = new RequestInfoDto();
        if (requestInfo.Data != null)
        {
            dto.Data = [.. requestInfo.Data];
        }
        dto.Status = requestInfo.Status switch
        {
            RequestStatus.IN_PROGRESS_PARTIAL_READY => RequestStatus.IN_PROGRESS,
            _ => requestInfo.Status
        };
        return dto;
    }
}
