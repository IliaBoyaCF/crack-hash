using Manager.Abstractions.Model;

namespace Manager.Abstractions.Services;

public interface ITaskScheduler
{
    Task ScheduleAsync(IEnumerable<IWorkerTask> tasks);
}
