using Manager.Abstractions.Model;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Extensions.Repository.Models;

namespace Manager.Abstractions.Entities;

public class WorkerTaskEntity : MongoEntity
{
    [BsonId]
    public WorkerTaskId Id { get; set; }

    public string Hash { get; set; }
    public RequestStatus Status { get; set; }
    public Uri WorkerAddress { get; set; }
    public bool IsTimeoutEnabled { get; set; }
    public TimeSpan TimeoutInterval { get; set; }
    public DateTime StartedAt { get; set; }
}

public class WorkerTaskId
{
    public string RequestId { get; set; }
    public int PartNumber { get; set; }
    public int PartCount { get; set; }
    public override string ToString()
    {
        return $"{RequestId}_{PartNumber}_{PartCount}";
    }
}
