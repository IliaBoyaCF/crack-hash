using Manager.Abstractions.Model;

namespace Manager.Abstractions.Services;

public interface ITaskStorage : IDictionary<string, List<IWorkerTask>>
{
}
