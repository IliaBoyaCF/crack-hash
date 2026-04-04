using Manager.Abstractions.Model;
using MongoDB.Entities;

namespace Manager.Service.Entities;

public class WorkerTaskEntity : Entity
{
    
    public WorkerTaskId TaskId { get; set; }

    public int MaxLength { get; set; }

    public string[] Alphabet { get; set; }

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
    public override string ToString()
    {
        return $"{RequestId}_{PartNumber}";
    }
}
