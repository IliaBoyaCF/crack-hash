using Manager.Abstractions.Model;

namespace Manager.Api.Dtos;

public class RequestInfoDto
{
    public RequestStatus Status { get; set; }
    public IEnumerable<string>? Data { get; set; } = null;

}
