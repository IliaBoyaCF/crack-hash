using Manager.Abstractions.Model;

namespace Manager.Abstractions.Services;

public interface ITaskStorage : IStorage<string, List<IWorkerTask>>
{
}
