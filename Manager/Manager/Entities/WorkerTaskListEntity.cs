using MongoDB.Entities;

namespace Manager.Service.Entities;

public class WorkerTaskListEntity : Entity
{
    public List<WorkerTaskEntity> WorkerTasks { get; set; }
}
