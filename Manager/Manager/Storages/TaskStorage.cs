using Manager.Abstractions.Model;
using Manager.Abstractions.Services;

namespace Manager.Service.Storages;

public class TaskStorage : InMemoryStorage<string, List<IWorkerTask>>, ITaskStorage
{
}
