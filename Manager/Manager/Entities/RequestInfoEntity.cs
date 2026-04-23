using Manager.Abstractions.Model;
using MongoDB.Entities;

namespace Manager.Service.Entities;

public class RequestInfoEntity : Entity
{
    public required string Hash { get; set; }
    public int MaxLength { get; set; }
    public RequestStatus Status { get; set; }
    public List<string>? Data { get; set; }
    public bool IsTimeoutEnabled { get; set; }
    public TimeSpan TimeoutInterval { get; set; }
    public DateTime StartedAt { get; set; }
}
