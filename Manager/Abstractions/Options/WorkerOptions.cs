using Manager.Abstractions.Model;

namespace Manager.Abstractions.Options;

public class WorkerOptions
{
    public const string SectionName = "Workers";

    public List<WorkerDescription> Instances { get; set; } = [];
}
