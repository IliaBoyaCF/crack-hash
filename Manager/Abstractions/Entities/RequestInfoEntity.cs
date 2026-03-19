using Manager.Abstractions.Model;
using MongoDB.Entities;
using MongoDB.Extensions.Repository.Models;

namespace Manager.Abstractions.Entities;

public class RequestInfoEntity : MongoEntity
{
    public string Id { get; set; }
    public string Hash { get; set; }
    public RequestStatus Status { get; set; }
    public List<string>? Data { get; set; }
    public bool IsTimeoutEnabled { get; set; }
    public TimeSpan TimeoutInterval { get; set; }
    public DateTime StartedAt { get; set; }
}
